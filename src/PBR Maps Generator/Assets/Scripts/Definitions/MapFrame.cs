using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// A struct to hold the map frame components.
/// The map frame displays a map image, label, and button for each PBR map.
/// </summary>
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