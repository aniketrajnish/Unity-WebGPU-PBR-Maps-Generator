using UnityEngine;

public static class MetallicMap
{
    private static ComputeShader _metallicComp;
    private static int _kernelIdx;

    static MetallicMap()
    {
        try
        {
            _metallicComp = Resources.Load<ComputeShader>("MetallicCompute");
            if (_metallicComp == null)
            {
                throw new System.NullReferenceException("Failed to load Metallic compute shader. Make sure 'MetallicCompute.compute' is in a Resources folder.");
            }

            _kernelIdx = _metallicComp.FindKernel("CSMain");
            if (_kernelIdx < 0)
            {
                throw new System.InvalidOperationException("Failed to find 'CSMain' kernel in Metallic compute shader.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in MetallicMap initialization: {e.Message}");
            _metallicComp = null;
        }
    }

    public static Texture2D GPUConvertToMetallicMap(Texture2D baseMap)
    {
        if (_metallicComp == null)
            return CPUConvertToMetallicMap(baseMap);

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

        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _metallicComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        Texture2D metallicMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
        RenderTexture.active = metallicRT;
        metallicMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        metallicMap.Apply();

        RenderTexture.active = null;
        baseRT.Release();
        metallicRT.Release();

        return metallicMap;
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