using UnityEngine;
public static class SpecularMap
{
    public static Texture2D ConvertToSpecularMap(Texture2D baseMap)
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
