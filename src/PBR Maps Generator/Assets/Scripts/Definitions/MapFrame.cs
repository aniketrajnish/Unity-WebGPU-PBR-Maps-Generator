using UnityEngine;
using TMPro;
using UnityEngine.UI;
public struct MapFrame
{
    public Image mapImage;
    public TextMeshProUGUI mapLabelField;
    public Button downloadBtn;
    public MapFrame(Image _mapImage, TextMeshProUGUI _mapLabelField, Button _downloadBtn)
    {
        mapImage = _mapImage;
        mapLabelField = _mapLabelField;
        downloadBtn = _downloadBtn;
    }
}