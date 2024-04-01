using UnityEngine;
public static class DiffuseMap
{
    public static Texture2D ConvertToDiffuseMap(Texture2D baseMap)
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