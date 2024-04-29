using System.Collections;
using System.Collections.Generic;
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
