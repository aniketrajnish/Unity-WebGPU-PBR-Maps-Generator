using UnityEngine;
public static class NormalMap
{
    public static Texture2D ConvertToNormalMap(Texture2D baseMap)
    {
        Texture2D heightMap = HeightMap.ConvertToHeightMap(baseMap);

        Texture2D normalMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);

        for (int y = 0; y < heightMap.height; y++)
        {
            for (int x = 0; x < heightMap.width; x++)
            {
                float left = GetPixelHeight(heightMap, x - 1, y);
                float right = GetPixelHeight(heightMap, x + 1, y);
                float up = GetPixelHeight(heightMap, x, y - 1);
                float down = GetPixelHeight(heightMap, x, y + 1);

                Vector3 normal = new Vector3(left - right, up - down, 2f).normalized;
                normal = (normal + Vector3.one) * .5f;

                normalMap.SetPixel(x, y, new Color(normal.x, normal.y, normal.z, 1f));
            }
        }
        normalMap.Apply();
        return normalMap;
    }
    public static float GetPixelHeight(Texture2D tex, int x, int y)
    {
        x = Mathf.Clamp(x, 0, tex.width - 1);
        y = Mathf.Clamp(y, 0, tex.height - 1);

        return tex.GetPixel(x, y).grayscale;
    }
}