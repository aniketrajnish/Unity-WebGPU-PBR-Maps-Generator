using UnityEngine;

public static class GlossinessMap
{
    private static ComputeShader _glossinessComp;
    private static int _kernelIdx;

    static GlossinessMap()
    {
        try
        {
            _glossinessComp = Resources.Load<ComputeShader>("GlossinessCompute");
            if (_glossinessComp == null)
            {
                throw new System.NullReferenceException("Failed to load Glossiness compute shader. Make sure 'GlossinessCompute.compute' is in a Resources folder.");
            }

            _kernelIdx = _glossinessComp.FindKernel("CSMain");
            if (_kernelIdx < 0)
            {
                throw new System.InvalidOperationException("Failed to find 'CSMain' kernel in Glossiness compute shader.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in GlossinessMap initialization: {e.Message}");
            _glossinessComp = null;
        }
    }

    public static Texture2D GPUConvertToGlossinessMap(Texture2D baseMap)
    {
        //if (_glossinessComp == null)
            return CPUConvertToGlossinessMap(baseMap);

        Texture2D roughnessMap = RoughnessMap.GPUConvertToRoughnessMap(baseMap);

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

        Texture2D glossinessMap = new Texture2D(w, h, TextureFormat.ARGB32, false);
        RenderTexture.active = glossinessRT;
        glossinessMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        glossinessMap.Apply();

        RenderTexture.active = null;
        roughnessRT.Release();
        glossinessRT.Release();

        return glossinessMap;
    }

    private static Texture2D CPUConvertToGlossinessMap(Texture2D baseMap)
    {
        Texture2D roughnessMap = RoughnessMap.GPUConvertToRoughnessMap(baseMap);
        Texture2D glossinessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);
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