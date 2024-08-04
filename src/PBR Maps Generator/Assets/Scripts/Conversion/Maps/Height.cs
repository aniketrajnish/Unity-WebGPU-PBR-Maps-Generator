using UnityEngine;
using UnityEngine.Rendering;
using System;
using Unity.VisualScripting;

public static class HeightMap
{
    private static ComputeShader _heightComp;
    private static int _kernelIdx;

    static HeightMap()
    {
        bool _useGPU = GPUUtility.IsGPUComputeAvailable();
        Debug.Log($"GPU Compute is {( _useGPU ? "available" : "not available" )}");
        if (_useGPU)
            InitializeComputeShader();        
    }
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

    public static void GPUConvertToHeightMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        if (_heightComp == null)
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