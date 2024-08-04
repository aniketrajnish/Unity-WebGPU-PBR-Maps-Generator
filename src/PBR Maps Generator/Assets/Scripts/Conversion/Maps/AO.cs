using UnityEngine;
using UnityEngine.Rendering;
using System;

public static class AOMap
{
    private static ComputeShader _aoComp;
    private static int _kernelIdx;

    static AOMap()
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

    public static void GPUConvertToAOMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        if (_aoComp == null)
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

    private static Texture2D CPUConvertToAOMap(Texture2D baseMap)
    {
        // CPU conversion method remains unchanged
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