using UnityEngine;
using UnityEngine.Rendering;
using System;

public static class GlossinessMap
{
    private static ComputeShader _glossinessComp;
    private static int _kernelIdx;

    static GlossinessMap()
    {
        bool _useGPU = GPUUtility.IsGPUComputeAvailable();
        Debug.Log($"GPU Compute is {(_useGPU ? "available" : "not available")}");
        if (_useGPU)
            InitializeComputeShader();        
    }
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
    public static void GPUConvertToGlossinessMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        if (_glossinessComp == null)
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