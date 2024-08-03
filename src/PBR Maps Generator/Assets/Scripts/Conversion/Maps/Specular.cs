using UnityEngine;

public static class SpecularMap
{
    private static ComputeShader _specularComp;
    private static int _kernelIdx;

    static SpecularMap()
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

    public static Texture2D GPUConvertToSpecularMap(Texture2D baseMap)
    {
        if (_specularComp == null)
            return CPUConvertToSpecularMap(baseMap);

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

        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _specularComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        Texture2D specularMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
        RenderTexture.active = specularRT;
        specularMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        specularMap.Apply();

        RenderTexture.active = null;
        baseRT.Release();
        specularRT.Release();

        return specularMap;
    }

    private static Texture2D CPUConvertToSpecularMap(Texture2D baseMap)
    {
        Texture2D specularMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);
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