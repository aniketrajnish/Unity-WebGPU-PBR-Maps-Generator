using UnityEngine;

public static class RoughnessMap
{
    private static ComputeShader _roughnessComp;
    private static int _kernelIdx;

    static RoughnessMap()
    {
        try
        {
            _roughnessComp = Resources.Load<ComputeShader>("RoughnessCompute");
            if (_roughnessComp == null)
            {
                throw new System.NullReferenceException("Failed to load Roughness compute shader. Make sure 'RoughnessCompute.compute' is in a Resources folder.");
            }

            _kernelIdx = _roughnessComp.FindKernel("CSMain");
            if (_kernelIdx < 0)
            {
                throw new System.InvalidOperationException("Failed to find 'CSMain' kernel in Roughness compute shader.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in RoughnessMap initialization: {e.Message}");
            _roughnessComp = null;
        }
    }

    public static Texture2D GPUConvertToRoughnessMap(Texture2D baseMap)
    {
        //if (_roughnessComp == null)
            return CPUConvertToRoughnessMap(baseMap);

        int w = baseMap.width;
        int h = baseMap.height;

        RenderTexture baseRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        baseRT.enableRandomWrite = true;
        Graphics.Blit(baseMap, baseRT);

        RenderTexture roughnessRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        roughnessRT.enableRandomWrite = true;
        roughnessRT.Create();

        _roughnessComp.SetTexture(_kernelIdx, "BaseMap", baseRT);
        _roughnessComp.SetTexture(_kernelIdx, "RoughnessMap", roughnessRT);
        _roughnessComp.SetInts("TextureSize", w, h);

        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _roughnessComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        Texture2D roughnessMap = new Texture2D(w, h, TextureFormat.ARGB32, false);
        RenderTexture.active = roughnessRT;
        roughnessMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        roughnessMap.Apply();

        RenderTexture.active = null;
        baseRT.Release();
        roughnessRT.Release();

        return roughnessMap;
    }

    private static Texture2D CPUConvertToRoughnessMap(Texture2D baseMap)
    {
        Texture2D roughnessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);
        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                float center = baseMap.GetPixel(x, y).grayscale, sum = 0;
                int count = 0;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;
                        int sampleX = Mathf.Clamp(x + offsetX, 0, baseMap.width - 1);
                        int sampleY = Mathf.Clamp(y + offsetY, 0, baseMap.height - 1);
                        sum += Mathf.Abs(baseMap.GetPixel(sampleX, sampleY).grayscale - center);
                        count++;
                    }
                }
                float roughness = Mathf.Clamp01(sum / count);
                roughnessMap.SetPixel(x, y, new Color(roughness, roughness, roughness));
            }
        }
        roughnessMap.Apply();
        return roughnessMap;
    }
}