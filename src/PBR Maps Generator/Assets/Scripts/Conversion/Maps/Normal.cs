using UnityEngine;

public static class NormalMap
{
    private static ComputeShader _normalComp;
    private static int _kernelIdx;

    static NormalMap()
    {
        try
        {
            _normalComp = Resources.Load<ComputeShader>("NormalCompute");
            if (_normalComp == null)
            {
                throw new System.NullReferenceException("Failed to load Normal compute shader. Make sure 'NormalCompute.compute' is in a Resources folder.");
            }

            _kernelIdx = _normalComp.FindKernel("CSMain");
            if (_kernelIdx < 0)
            {
                throw new System.InvalidOperationException("Failed to find 'CSMain' kernel in Normal compute shader.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in NormalMap initialization: {e.Message}");
            _normalComp = null;
        }
    }

    public static Texture2D GPUConvertToNormalMap(Texture2D baseMap)
    {
        if (_normalComp == null)
            return CPUConvertToNormalMap(baseMap);

        Texture2D heightMap = HeightMap.GPUConvertToHeightMap(baseMap);

        int w = baseMap.width;
        int h = baseMap.height;

        RenderTexture heightRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
        heightRT.enableRandomWrite = true;
        Graphics.Blit(heightMap, heightRT);

        RenderTexture normalRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        normalRT.enableRandomWrite = true;
        normalRT.Create();

        _normalComp.SetTexture(_kernelIdx, "HeightMap", heightRT);
        _normalComp.SetTexture(_kernelIdx, "NormalMap", normalRT);
        _normalComp.SetInts("TextureSize", w, h);

        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _normalComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        Texture2D normalMap = new Texture2D(w, h, TextureFormat.RGB24, false);
        RenderTexture.active = normalRT;
        normalMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        normalMap.Apply();

        RenderTexture.active = null;
        heightRT.Release();
        normalRT.Release();

        return normalMap;
    }

    private static Texture2D CPUConvertToNormalMap(Texture2D baseMap)
    {
        Texture2D heightMap = HeightMap.GPUConvertToHeightMap(baseMap);
        Texture2D normalMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);
        for (int y = 0; y < heightMap.height; y++)
        {
            for (int x = 0; x < heightMap.width; x++)
            {
                float left = GetPixelHeight(heightMap, x - 1, y);
                float right = GetPixelHeight(heightMap, x + 1, y);
                float up = GetPixelHeight(heightMap, x, y - 1);
                float down = GetPixelHeight(heightMap, x, y + 1);
                Vector3 normal = new Vector3(left - right, up - down, 2f).normalized * 5f;
                normal = (normal + Vector3.one) * .5f;
                normalMap.SetPixel(x, y, new Color(normal.x, normal.y, normal.z, 1f));
            }
        }
        normalMap.Apply();
        return normalMap;
    }

    private static float GetPixelHeight(Texture2D tex, int x, int y)
    {
        x = Mathf.Clamp(x, 0, tex.width - 1);
        y = Mathf.Clamp(y, 0, tex.height - 1);
        return tex.GetPixel(x, y).grayscale;
    }
}