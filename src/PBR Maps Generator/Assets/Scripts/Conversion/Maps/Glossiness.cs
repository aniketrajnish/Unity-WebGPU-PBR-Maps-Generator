using UnityEngine;
public static class GlossinessMap
{
    public static Texture2D ConvertToGlossinessMap(Texture2D baseMap)
    {
        Texture2D roughnessMap = RoughnessMap.ConvertToRoughnessMap(baseMap);
        Texture2D glossinessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);

        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                float glossiness = 1f - roughnessMap.GetPixel(x, y).grayscale;
                glossinessMap.SetPixel(x, y, new Color(glossiness, glossiness, glossiness));
            }
        }
        glossinessMap.Apply();
        return glossinessMap;
    }
}
