using UnityEngine;
public static class HeightMap
{
    public static Texture2D ConvertToHeightMap(Texture2D baseMap)
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