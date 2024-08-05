using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates a roughness map from a base map.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class RoughnessMap
{
    private static ComputeShader _roughnessComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static RoughnessMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }

    /// <summary>
    /// Load the Roughness compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {
        _roughnessComp = Resources.Load<ComputeShader>("RoughnessCompute");
        if (_roughnessComp == null)
        {
            Debug.LogError("Failed to load Roughness compute shader. Make sure 'RoughnessCompute.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _roughnessComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in Roughness compute shader.");
    }

    /// <summary>
    /// Perfrom the Roughness conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down.
    /// </summary>
    /// <!-- This is an async operation and will return the Roughness map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to Roughness.</param>
    /// <param name="callback">The callback to return the Roughness map.</param>
    public static void GPUConvertToRoughnessMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_roughnessComp == null || !_useGPU)
        {
            callback(CPUConvertToRoughnessMap(baseMap));
            return;
        }

        int w = baseMap.width;
        int h = baseMap.height;
        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);
        RenderTexture roughnessRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        roughnessRT.enableRandomWrite = true;
        roughnessRT.Create();
        _roughnessComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _roughnessComp.SetTexture(_kernelIdx, "RoughnessMap", roughnessRT);
        _roughnessComp.SetInts("TextureSize", w, h);
        _roughnessComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _roughnessComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        AsyncGPUReadback.Request(roughnessRT, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error detected.");
                callback(null);
                baseRT.Release();
                roughnessRT.Release();
                return;
            }

            Texture2D roughnessMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
            roughnessMap.LoadRawTextureData(request.GetData<byte>());
            roughnessMap.Apply();

            baseRT.Release();
            roughnessRT.Release();

            callback(roughnessMap);
        });
    }
    /// <summary>
    /// Takes a base map, iterate over each pixel,
    /// for each pixel, iterate over each of its neighbors, 
    /// calculate the sum of the absolute difference between the center pixel and its neighbors,
    /// divide the sum by the number of neighbors to get the roughness value.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <param name="baseMap">The base map (Texture2D) to convert to Roughness.</param>
    /// <returns>
    /// The Roughness map (Texture2D) generated from the base map.
    /// </returns>
    public static Texture2D CPUConvertToRoughnessMap(Texture2D baseMap)
    {
        Texture2D roughnessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                float center = baseMap.GetPixel(x, y).grayscale, sum = 0;
                int count = 0;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;
                        int sampleX = Mathf.Clamp(x + offsetX, 0, baseMap.width - 1);
                        int sampleY = Mathf.Clamp(y + offsetY, 0, baseMap.height - 1);
                        sum += Mathf.Abs(baseMap.GetPixel(sampleX, sampleY).grayscale - center);
                        count++;
                    }
                }
                float roughness = Mathf.Clamp01(sum / count);
                roughnessMap.SetPixel(x, y, new Color(roughness, roughness, roughness));
            }
        }
        roughnessMap.Apply();
        return roughnessMap;
    }
}