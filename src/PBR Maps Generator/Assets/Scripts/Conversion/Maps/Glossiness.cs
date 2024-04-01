using UnityEngine;
public static class GlossinessMap
{
    public static Texture2D ConvertToGlossinessMap(Texture2D baseMap)
    {
        Texture2D glossinessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);

        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                float center = baseMap.GetPixel(x, y).grayscale;
                float neighbor = baseMap.GetPixel(Mathf.Clamp(x + 1, 0, baseMap.width - 1), y).grayscale;

                float glossiness = Mathf.Clamp01(1 - Mathf.Abs(center - neighbor));
                glossinessMap.SetPixel(x, y, new Color(glossiness, glossiness, glossiness));
            }
        }
        glossinessMap.Apply();
        return glossinessMap;
    }
}
