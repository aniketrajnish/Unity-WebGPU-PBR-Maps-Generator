using UnityEngine;
public static class AOMap
{
    public static Texture2D ConvertToAOMap(Texture2D baseMap)
    {
        Texture2D aoMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);

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