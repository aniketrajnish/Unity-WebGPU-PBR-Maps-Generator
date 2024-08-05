// to make it artist-friendly ;)
using UnityEngine;

public class UX : MonoBehaviour
{
    [SerializeField] GameObject generationFrames, previewFrame, uploadText;

    public void OnImageUploaded()
    {
        generationFrames.SetActive(true);
        previewFrame.SetActive(true);
        uploadText.SetActive(false);
    }
}
