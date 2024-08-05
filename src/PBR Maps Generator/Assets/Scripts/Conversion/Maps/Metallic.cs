using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates a metallic map from a base map.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class MetallicMap
{
    private static ComputeShader _metallicComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static MetallicMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }

    /// <summary>
    /// Load the Metallic compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {
        _metallicComp = Resources.Load<ComputeShader>("MetallicCompute");
        if (_metallicComp == null)
        {
            Debug.LogError("Failed to load Metallic compute shader. Make sure 'MetallicCompute.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _metallicComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in Metallic compute shader.");
    }

    /// <summary>
    /// Perfrom the Metallic conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down.
    /// </summary>
    /// <!-- This is an async operation and will return the Metallic map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to Metallic.</param>
    /// <param name="callback">The callback to return the Metallic map.</param>
    public static void GPUConvertToMetallicMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_metallicComp == null || !_useGPU)
        {
            callback(CPUConvertToMetallicMap(baseMap));
            return;
        }

        int w = baseMap.width;
        int h = baseMap.height;
        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);
        RenderTexture metallicRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        metallicRT.enableRandomWrite = true;
        metallicRT.Create();
        _metallicComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _metallicComp.SetTexture(_kernelIdx, "MetallicMap", metallicRT);
        _metallicComp.SetInts("TextureSize", w, h);
        _metallicComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _metallicComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        AsyncGPUReadback.Request(metallicRT, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error detected.");
                callback(null);
                baseRT.Release();
                metallicRT.Release();
                return;
            }

            Texture2D metallicMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
            metallicMap.LoadRawTextureData(request.GetData<byte>());
            metallicMap.Apply();

            baseRT.Release();
            metallicRT.Release();

            callback(metallicMap);
        });
    }

    /// <summary>
    /// Takes a base map, and uses the saturation of each pixel to determine the metallic value.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <param name="baseMap">The base map (Texture2D) to convert to Metallic.</param>
    /// <returns>
    /// The Metallic map (Texture2D) generated from the base map.
    /// </returns>
    private static Texture2D CPUConvertToMetallicMap(Texture2D baseMap)
    {
        Texture2D metallicMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                Color baseCol = baseMap.GetPixel(x, y);
                Color.RGBToHSV(baseCol, out _, out float sat, out _);
                float metallic = Mathf.Clamp01(baseCol.grayscale * (1 - sat));
                metallicMap.SetPixel(x, y, new Color(metallic, metallic, metallic));
            }
        }
        metallicMap.Apply();
        return metallicMap;
    }
}