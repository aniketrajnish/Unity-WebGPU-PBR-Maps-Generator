using UnityEngine;
using TMPro;
using UnityEngine.UI;
public struct MapFrame
{
    public Image mapImage;
    public TextMeshProUGUI mapLabelField;
    public Button btn;
    public MapFrame(Image _mapImage, TextMeshProUGUI _mapLabelField, Button _btn)
    {
        mapImage = _mapImage;
        mapLabelField = _mapLabelField;
        btn = _btn;
    }
}