using UnityEngine;
public static class MetallicMap
{
    public static Texture2D ConvertToMetallicMap(Texture2D baseMap)
    {
        Texture2D metallicMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);

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
