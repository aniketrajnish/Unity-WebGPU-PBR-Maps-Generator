using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates a height map from a base map texture.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class HeightMap
{
    private static ComputeShader _heightComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static HeightMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }
    /// <summary>
    /// Load the Height compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {
        _heightComp = Resources.Load<ComputeShader>("HeightCompute");
        if (_heightComp == null)
        {
            Debug.LogError("Failed to load Height compute shader. Make sure 'Height.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _heightComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in Height compute shader.");
    }

    /// <summary>
    /// Perfrom the Height conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down.
    /// </summary>
    /// <!-- This is an async operation and will return the Height map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to Height.</param>
    /// <param name="callback">The callback to return the Height map.</param>
    public static void GPUConvertToHeightMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_heightComp == null || !_useGPU)
        {
            callback(CPUConvertToHeightMap(baseMap));
            return;
        }

        int w = baseMap.width;
        int h = baseMap.height;
        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);
        RenderTexture heightRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        heightRT.enableRandomWrite = true;
        heightRT.Create();
        _heightComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _heightComp.SetTexture(_kernelIdx, "HeightMap", heightRT);
        _heightComp.SetInts("TextureSize", w, h);
        _heightComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _heightComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        AsyncGPUReadback.Request(heightRT, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error detected.");
                callback(null);
                baseRT.Release();
                heightRT.Release();
                return;
            }

            Texture2D heightMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
            heightMap.LoadRawTextureData(request.GetData<byte>());
            heightMap.Apply();

            baseRT.Release();
            heightRT.Release();

            callback(heightMap);
        });
    }
    /// <summary>
    /// Takes a base map, iterates over each pixel and converts it to a grayscale value.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <parm name="baseMap">The base map to convert to Glossiness.</param>
    /// <returns>
    /// The Height map (Texture2D) generated from the base map.
    /// </returns>
    public static Texture2D CPUConvertToHeightMap(Texture2D baseMap)
    {
        Texture2D heightMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                Color baseCol = baseMap.GetPixel(x, y);
                float height = baseCol.grayscale;
                heightMap.SetPixel(x, y, new Color(height, height, height));
            }
        }
        heightMap.Apply();
        return heightMap;
    }
}