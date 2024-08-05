using UnityEngine;
using UnityEngine.Rendering;
using System;

public static class MetallicMap
{
    private static ComputeShader _metallicComp;
    private static int _kernelIdx;

    static MetallicMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }
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