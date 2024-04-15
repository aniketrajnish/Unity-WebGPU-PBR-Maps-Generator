using UnityEngine;
public static class FixTexture
{
    public static Texture2D UncompressAndExposeTexture(Texture2D source)
    {
        RenderTexture temp = RenderTexture.GetTemporary(
                                source.width,
                                source.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);

        Graphics.Blit(source, temp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temp;
        Texture2D fixedTex = new Texture2D(source.width, source.height);
        fixedTex.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        fixedTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temp);

        return fixedTex;
    }
}

