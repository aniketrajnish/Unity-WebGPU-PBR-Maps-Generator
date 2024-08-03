using UnityEngine;

public static class DiffuseMap
{
    private static ComputeShader _diffuseComp;
    private static int _kernelIdx;

    static DiffuseMap()
    {
        try
        {
            _diffuseComp = Resources.Load<ComputeShader>("DiffuseCompute");
            if (_diffuseComp == null)
            {
                throw new System.NullReferenceException("Failed to load Diffuse compute shader. Make sure 'DiffuseCompute.compute' is in a Resources folder.");
            }

            _kernelIdx = _diffuseComp.FindKernel("CSMain");
            if (_kernelIdx < 0)
            {
                throw new System.InvalidOperationException("Failed to find 'CSMain' kernel in Diffuse compute shader.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in DiffuseMap initialization: {e.Message}");
            _diffuseComp = null;
        }
    }

    public static Texture2D GPUConvertToDiffuseMap(Texture2D baseMap)
    {
        if (_diffuseComp == null)
            return CPUConvertToDiffuseMap(baseMap);

        int w = baseMap.width;
        int h = baseMap.height;

        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);

        RenderTexture diffuseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        diffuseRT.enableRandomWrite = true;
        diffuseRT.Create();

        _diffuseComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _diffuseComp.SetTexture(_kernelIdx, "DiffuseMap", diffuseRT);

        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _diffuseComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        Texture2D diffuseMap = new Texture2D(w, h, TextureFormat.RGB24, false);
        RenderTexture.active = diffuseRT;
        diffuseMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        diffuseMap.Apply();

        RenderTexture.active = null;
        baseRT.Release();
        diffuseRT.Release();

        return diffuseMap;
    }

    private static Texture2D CPUConvertToDiffuseMap(Texture2D baseMap)
    {
        Texture2D diffuseMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                Color pixel = baseMap.GetPixel(x, y);
                float luminance = pixel.r * 0.2126f + pixel.g * 0.7152f + pixel.b * 0.0722f;
                diffuseMap.SetPixel(x, y, new Color(luminance, luminance, luminance));
            }
        }
        diffuseMap.Apply();
        return diffuseMap;
    }
}