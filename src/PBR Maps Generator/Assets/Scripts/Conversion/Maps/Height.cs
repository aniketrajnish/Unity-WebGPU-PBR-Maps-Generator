using UnityEngine;

public static class HeightMap
{
    private static ComputeShader _heightComp;
    private static int _kernelIdx;

    static HeightMap()
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

    public static Texture2D GPUConvertToHeightMap(Texture2D baseMap)
    {
        //if (_heightComp == null)        
            return CPUConvertToHeightMap(baseMap);        

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

        int thrdX = Mathf.CeilToInt(w / 8.0f);
        int thrdY = Mathf.CeilToInt(h / 8.0f);
        _heightComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

        Texture2D heightMap = new Texture2D(w, h, TextureFormat.ARGB32, false);
        RenderTexture.active = heightRT;
        heightMap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        heightMap.Apply();

        RenderTexture.active = null;
        baseRT.Release();
        heightRT.Release();

        return heightMap;
    }

    private static Texture2D CPUConvertToHeightMap(Texture2D baseMap)
    {
        Texture2D heightMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);
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