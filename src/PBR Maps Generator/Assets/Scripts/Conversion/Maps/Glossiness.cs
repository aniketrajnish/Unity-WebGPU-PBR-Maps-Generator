using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates a glossiness map from a base map.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class GlossinessMap
{    
    private static ComputeShader _glossinessComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static GlossinessMap()
    {        
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }

    /// <summary>
    /// Load the Glossiness compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {        
        _glossinessComp = Resources.Load<ComputeShader>("GlossinessCompute");
        if (_glossinessComp == null)
        {
            Debug.LogError("Failed to load Glossiness compute shader. Make sure 'GlossinessCompute.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _glossinessComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in Glossiness compute shader.");
    }

    /// <summary>
    /// Perfrom the Glossiness conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down. 
    /// </summary>
    /// <!-- This is an async operation and will return the Glossiness map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to Glossiness.</param>"
    /// <param name="callback">The callback to return the Glossiness map.</param>"
    public static void GPUConvertToGlossinessMap(Texture2D baseMap, Action<Texture2D> callback)
    {       
        bool _useGPU = GPUUtility.useGPU;
        if (_glossinessComp == null || !_useGPU)
        {
            callback(CPUConvertToGlossinessMap(baseMap));
            return;
        }

        RoughnessMap.GPUConvertToRoughnessMap(baseMap, (roughnessMap) =>
        {
            if (roughnessMap == null)
            {
                Debug.LogError("Failed to generate roughness map");
                callback(null);
                return;
            }

            int w = baseMap.width;
            int h = baseMap.height;
            RenderTexture roughnessRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            roughnessRT.enableRandomWrite = true;
            Graphics.Blit(roughnessMap, roughnessRT);
            RenderTexture glossinessRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            glossinessRT.enableRandomWrite = true;
            glossinessRT.Create();
            _glossinessComp.SetTexture(_kernelIdx, "RoughnessMap", roughnessRT);
            _glossinessComp.SetTexture(_kernelIdx, "GlossinessMap", glossinessRT);
            _glossinessComp.SetInts("TextureSize", w, h);
            _glossinessComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
            int thrdX = Mathf.CeilToInt(w / 8.0f);
            int thrdY = Mathf.CeilToInt(h / 8.0f);
            _glossinessComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

            AsyncGPUReadback.Request(glossinessRT, 0, TextureFormat.RGBA32, (request) =>
            {
                if (request.hasError)
                {
                    Debug.LogError("GPU readback error detected.");
                    callback(null);
                    roughnessRT.Release();
                    glossinessRT.Release();
                    return;
                }

                Texture2D glossinessMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
                glossinessMap.LoadRawTextureData(request.GetData<byte>());
                glossinessMap.Apply();

                roughnessRT.Release();
                glossinessRT.Release();

                callback(glossinessMap);
            });
        });
    }

    /// <summary>
    /// Take a base map, convert to roughness, invert the values and return as glossiness.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <parm name="baseMap">The base map to convert to Glossiness.</param>
    /// <returns>
    /// The Glossiness map (Texture2D) generated from the base map.
    /// </returns>
    private static Texture2D CPUConvertToGlossinessMap(Texture2D baseMap)
    {       
        Texture2D roughnessMap = RoughnessMap.CPUConvertToRoughnessMap(baseMap);
        Texture2D glossinessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                float glossiness = 1f - roughnessMap.GetPixel(x, y).grayscale;
                glossinessMap.SetPixel(x, y, new Color(glossiness, glossiness, glossiness));
            }
        }
        glossinessMap.Apply();
        return glossinessMap;
    }
}