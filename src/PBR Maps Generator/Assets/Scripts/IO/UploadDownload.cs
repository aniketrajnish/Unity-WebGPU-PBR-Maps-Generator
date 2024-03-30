using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

public class UploadDownload : MonoBehaviour
{
    [SerializeField] private Image baseMapHolder;

    [DllImport("__Internal")]
    private static extern void UploadImage();

    public void OnUploadBtnClick()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            UploadImage();        
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
}
