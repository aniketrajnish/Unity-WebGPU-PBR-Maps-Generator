using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates a specular map from a base map.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class SpecularMap
{
    private static ComputeShader _specularComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static SpecularMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }

    /// <summary>
    /// Load the Specular compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {
        _specularComp = Resources.Load<ComputeShader>("SpecularCompute");
        if (_specularComp == null)
        {
            Debug.LogError("Failed to load Specular compute shader. Make sure 'SpecularCompute.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _specularComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in Specular compute shader.");
    }

    /// <summary>
    /// Perfrom the Specular conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down.
    /// </summary>
    /// <!-- This is an async operation and will return the Specular map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to Specular.</param>
    /// <param name="callback">The callback to return the Specular map.</param>
    public static void GPUConvertToSpecularMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_specularComp == null || !_useGPU)
        {
            callback(CPUConvertToSpecularMap(baseMap));
            return;
        }

        int w = baseMap.width;
        int h = baseMap.height;
        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);
        RenderTexture specularRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        specularRT.enableRandomWrite = true;
        specularRT.Create();
        _specularComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _specularComp.SetTexture(_kernelIdx, "SpecularMap", specularRT);
        _specularComp.SetInts("TextureSize", w, h);
        _specularComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _specularComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        AsyncGPUReadback.Request(specularRT, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error detected.");
                callback(null);
                baseRT.Release();
                specularRT.Release();
                return;
            }

            Texture2D specularMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
            specularMap.LoadRawTextureData(request.GetData<byte>());
            specularMap.Apply();

            baseRT.Release();
            specularRT.Release();

            callback(specularMap);
        });
    }
    /// <summary>
    /// Takes a base map, iterate over each pixel,
    /// calculate the reflectivity based on the grayscale value of the pixel,
    /// multiply the base color by the reflectivity to get the specular color.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <param name="baseMap">The base map (Texture2D) to convert to Specular.</param>
    /// <returns>
    /// The Specular map (Texture2D) generated from the base map.
    /// </returns>
    private static Texture2D CPUConvertToSpecularMap(Texture2D baseMap)
    {
        Texture2D specularMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                Color baseCol = baseMap.GetPixel(x, y);
                float reflectivity = Mathf.Clamp01(baseCol.grayscale + .5f);
                Color specCol = new Color(baseCol.r * reflectivity, baseCol.g * reflectivity, baseCol.b * reflectivity);
                specularMap.SetPixel(x, y, specCol);
            }
        }
        specularMap.Apply();
        return specularMap;
    }
}