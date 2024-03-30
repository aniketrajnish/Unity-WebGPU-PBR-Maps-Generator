using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

public class UploadDownload : MonoBehaviour
{
    [SerializeField] private Image baseMapHolder;

    [DllImport("__Internal")]
    private static extern void UploadImage();
    [DllImport("__Internal")]
    private static extern void DownloadImage(string base64Data, string fileName);

    public void OnUploadBtnClick()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            UploadImage();        
    }
    public void OnDownloadBtnClick()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            DownloadImage(baseMapHolder.sprite.texture);        
    }
    public void LoadImage(string imgDataUrl)
    {
        string base64 = imgDataUrl.Split(',')[1];
        byte[] imgBytes = System.Convert.FromBase64String(base64);
        Texture2D tex = new Texture2D(2, 2);

        if (tex.LoadImage(imgBytes))        
            baseMapHolder.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));        
        else 
            Debug.LogError("Failed to load image");
    }
    public void DownloadImage(Texture2D imageToDownload)
    {
        byte[] imgBytes = imageToDownload.EncodeToPNG();
        string base64img = System.Convert.ToBase64String(imgBytes);

        DownloadImage(base64img, "image.png");
    }

}
