using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor; // Editor namespace for loading and saving files
#endif

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
#if UNITY_EDITOR
        else
        {
            string path = EditorUtility.OpenFilePanel("Load image", "", "png,jpg,jpeg,tif,tiff");
            if (!string.IsNullOrEmpty(path))
            {
                LoadImageFromFile(path);
            }
        }
#endif
    }
    public void OnDownloadBtnClick(string fileName, string extension)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            DownloadImage(baseMapHolder.sprite.texture);
#if UNITY_EDITOR
        else if (baseMapHolder.sprite.texture != null)
        {
            string path = EditorUtility.SaveFilePanel("Save image as PNG", "", fileName, extension);
            if (!string.IsNullOrEmpty(path))
            {
                Texture2D imageToDownload = baseMapHolder.sprite.texture as Texture2D;
                byte[] imgBytes = imageToDownload.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, imgBytes);
                Debug.Log("Saved image to " + path);
            }
        }
#endif
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
#if UNITY_EDITOR
    private void LoadImageFromFile(string filePath)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData)) // Automatically resizes the texture dimensions.
            baseMapHolder.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        else
            Debug.LogError("Failed to load image from file");
    }
#endif
}
