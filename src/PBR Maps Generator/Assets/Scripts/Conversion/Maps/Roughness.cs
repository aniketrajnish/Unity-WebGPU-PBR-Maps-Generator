using UnityEngine;
public static class RougnnessMap
{
    public static Texture2D ConvertToRoughnessMap(Texture2D baseMap)
    {
        Texture2D roughnessMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGB24, true);

        for (int y = 0; y < baseMap.height; y++)
        {
            for (int x = 0; x < baseMap.width; x++)
            {
                float center = baseMap.GetPixel(x, y).grayscale, sum = 0;
                int count = 0;

                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;

                        int sampleX = Mathf.Clamp(x + offsetX, 0, baseMap.width - 1);
                        int sampleY = Mathf.Clamp(y + offsetY, 0, baseMap.height - 1);

                        sum += Mathf.Abs(baseMap.GetPixel(sampleX, sampleY).grayscale - center);
                        count++;
                    }
                }
                float roungness = Mathf.Clamp01(sum / count);
                roughnessMap.SetPixel(x, y, new Color(roungness, roungness, roungness));
            }
        }
        roughnessMap.Apply();
        return roughnessMap;
    }
}
