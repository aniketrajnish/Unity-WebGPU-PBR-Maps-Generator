using UnityEngine;
using UnityEngine.Rendering;
using System;

public static class SpecularMap
{
    private static ComputeShader _specularComp;
    private static int _kernelIdx;

    static SpecularMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }
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