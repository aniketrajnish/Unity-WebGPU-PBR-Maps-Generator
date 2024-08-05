using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates an ambient occlusion map from a base map.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class AOMap    
{
    private static ComputeShader _aoComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static AOMap()
    {        
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();          
    }

    /// <summary>
    /// Load the AO compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {        
        _aoComp = Resources.Load<ComputeShader>("AOCompute");
        if (_aoComp == null)
        {
            Debug.LogError("Failed to load AO compute shader. Make sure 'AOCompute.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _aoComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in AO compute shader.");
    }

    /// <summary>
    /// Perfrom the AO conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down.
    /// </summary>
    /// <!-- This is an async operation and will return the AO map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to AO.</param>
    /// <param name="callback">The callback to return the AO map.</param>
    public static void GPUConvertToAOMap(Texture2D baseMap, Action<Texture2D> callback)
    {       
        bool _useGPU = GPUUtility.useGPU;
        if (_aoComp == null || !_useGPU)
        {
            callback(CPUConvertToAOMap(baseMap));
            return;
        }

        int w = baseMap.width;
        int h = baseMap.height;
        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);
        RenderTexture aoRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        aoRT.enableRandomWrite = true;
        aoRT.Create();
        _aoComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _aoComp.SetTexture(_kernelIdx, "AOMap", aoRT);
        _aoComp.SetInts("TextureSize", w, h);
        _aoComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _aoComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        AsyncGPUReadback.Request(aoRT, 0, TextureFormat.RGBA32, (request) =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error detected.");
                callback(null);
                baseRT.Release();
                aoRT.Release();
                return;
            }

            Texture2D aoMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
            aoMap.LoadRawTextureData(request.GetData<byte>());
            aoMap.Apply();

            baseRT.Release();
            aoRT.Release();

            callback(aoMap);
        });
    }

    /// <summary>
    /// Take a base map, iterate through each pixel, convert to grayscale and invert the values to get AO.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <param name="baseMap">The base map to convert to AO.</param>
    /// <returns>
    /// The AO map (Texture2D) generated from the base map.
    /// </returns>
    private static Texture2D CPUConvertToAOMap(Texture2D baseMap)
    {       
        Texture2D aoMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                Color baseCol = baseMap.GetPixel(x, y);
                float ao = 1 - baseCol.grayscale;
                aoMap.SetPixel(x, y, new Color(ao, ao, ao));
            }
        }
        aoMap.Apply();
        return aoMap;
    }
}