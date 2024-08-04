using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor; 
#endif

public class UploadDownload : MonoBehaviour
{
    public Image uploadImgHolder;
    public string uploadImgFileName, uploadImgExtension;
    public event Action<Texture2D> OnImageLoaded;

    [DllImport("__Internal")]
    private static extern void jsUploadImage();
    [DllImport("__Internal")]
    private static extern void jsDownloadImage(string base64Data, string fileName);
    public void OnUploadBtnClick()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            jsUploadImage();
#if UNITY_EDITOR
        else
        {
            string path = EditorUtility.OpenFilePanel("Load image", "", "png,jpg,jpeg");

            if (!string.IsNullOrEmpty(path))            
                LoadImageFromFile(path);            
        }
#endif
    }
    public void OnDownloadBtnClick(string fileName, string extension, Texture2D downloadTex)
    {
        print(Application.platform);
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            DownloadImage(downloadTex, fileName, extension);
#if UNITY_EDITOR
        else if (uploadImgHolder.sprite.texture != null)
        {
            string path = EditorUtility.SaveFilePanel("Save Image", "", fileName, extension);
            print(fileName);

            if (!string.IsNullOrEmpty(path))
                SaveImageToFile(path, extension, downloadTex);            
        }
#endif
    }
    public void LoadImage(string data)
    {
        string[] parts = data.Split('|');

        if (parts.Length == 2)
        {
            string imgDataUrl = parts[0];
            string fileName = parts[1];

            string base64 = imgDataUrl.Split(',')[1];
            byte[] imgBytes = System.Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2);

            if (tex.LoadImage(imgBytes))
            {
                uploadImgHolder.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                uploadImgFileName = Path.GetFileNameWithoutExtension(fileName);
                uploadImgExtension = Path.GetExtension(fileName).TrimStart('.');
                OnImageLoaded?.Invoke(tex);
            }
            else
                Debug.LogError("Failed to load image");
        }
        else
        {
            Debug.LogError("Error processing uploaded file data");
        }
    }
    public void DownloadImage(Texture2D imgToDownload, string fileName, string extension)
    {
        byte[] imgBytes = null;

        switch (extension)
        {
            case "png":
                imgBytes = imgToDownload.EncodeToPNG();
                break;
            case "jpg":
                imgBytes = imgToDownload.EncodeToJPG();
                break;
            case "jpeg":
                imgBytes = imgToDownload.EncodeToJPG();
                break;
            default:
                Debug.LogError("Invalid extension");
                break;
        }
        if (imgBytes != null)
        {
            string base64img = System.Convert.ToBase64String(imgBytes);
            jsDownloadImage(base64img, fileName + "." + extension);
        }
    }
#if UNITY_EDITOR
    private void LoadImageFromFile(string filePath)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData)) // Automatically resizes the texture dimensions.
        {
            uploadImgHolder.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            uploadImgFileName = Path.GetFileNameWithoutExtension(filePath);
            uploadImgExtension = Path.GetExtension(filePath).TrimStart('.');
            OnImageLoaded?.Invoke(tex);
        }
        else
            Debug.LogError("Failed to load image from file");
    }
    private void SaveImageToFile(string filePath, string extension, Texture2D downloadTex)
    {
        byte[] imgBytes = null;

        switch (extension)
        {
            case "png":
                imgBytes = downloadTex.EncodeToPNG();
                break;
            case "jpg":
                imgBytes = downloadTex.EncodeToJPG();
                break;
            case "jpeg":
                imgBytes = downloadTex.EncodeToJPG();
                break;
            default:
                Debug.LogError("Invalid extension");
                break;
        }
        if (imgBytes != null)
        {
            System.IO.File.WriteAllBytes(filePath, imgBytes);
            Debug.Log("Saved image to " + filePath);
        }
    }
#endif
}
