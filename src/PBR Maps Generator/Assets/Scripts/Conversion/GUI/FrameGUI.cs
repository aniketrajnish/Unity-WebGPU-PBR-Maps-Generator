using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
public class WindowGUI : MonoBehaviour
{
    [SerializeField] List<GameObject> mapFrameGOs;
    List<MapFrame> mapFrames;
    Dictionary<string, InputMaps> inputMapLabels;
    Dictionary<string, CommonMaps> commonMapLabels;
    Dictionary<string, MRMaps> mrMapLabels;
    Dictionary<string, SGMaps> sgMapLabels;
    [HideInInspector] public InputMaps currentInput = InputMaps.BASE;
    private void Start()
    {
        CreateMapLabelDictionaries();
        InitializeFrames();
        AssignLabels(currentInput);
    }
    void AssignLabels(InputMaps _currentInput)
    {
        mapFrames[0].mapLabelField.text = EnumString(_currentInput.ToString());

        switch (_currentInput)
        {
            case InputMaps.BASE:
                for (int i = 4; i < mapFrameGOs.Count; i++)    
                    mapFrames[i].mapLabelField.text = EnumString(mrMapLabels.ElementAt(i - 4).Key);
                break;
            case InputMaps.DIFFUSE: 
                for (int i = 4; i < mapFrameGOs.Count; i++)
                    mapFrames[i].mapLabelField.text = EnumString(sgMapLabels.ElementAt(i - 4).Key);                
                break;
        }
    }
    void InitializeFrames()
    {
        mapFrames = new List<MapFrame>();

        foreach (GameObject go in mapFrameGOs)
        {
            Image mapImage = go.transform.GetComponentsInChildren<Image>()[1];
            TextMeshProUGUI mapLabelField = go.transform.GetComponentInChildren<TextMeshProUGUI>();
            Button downloadBtn = go.transform.GetComponentInChildren<Button>();

            mapFrames.Add(new MapFrame(mapImage, mapLabelField, downloadBtn));
        }
    }
    void CreateMapLabelDictionaries()
    {
        inputMapLabels = new Dictionary<string, InputMaps>();
        foreach (InputMaps map in System.Enum.GetValues(typeof(InputMaps)))
            inputMapLabels.Add(EnumString(map.ToString()), map);

        commonMapLabels = new Dictionary<string, CommonMaps>();
        foreach (CommonMaps map in System.Enum.GetValues(typeof(CommonMaps)))
            commonMapLabels.Add(EnumString(map.ToString()), map);

        mrMapLabels = new Dictionary<string, MRMaps>();
        foreach (MRMaps map in System.Enum.GetValues(typeof(MRMaps)))
            mrMapLabels.Add(EnumString(map.ToString()), map);

        sgMapLabels = new Dictionary<string, SGMaps>();
        foreach (SGMaps map in System.Enum.GetValues(typeof(SGMaps)))
            sgMapLabels.Add(EnumString(map.ToString()), map);
    }

    public void OnMRToggleClicked() => AssignLabels(currentInput = InputMaps.BASE);
    public void OnSGToggleClicked() => AssignLabels(currentInput = InputMaps.DIFFUSE);
    string EnumString(string enumName) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enumName.ToLower().Replace("_", " "));
}
