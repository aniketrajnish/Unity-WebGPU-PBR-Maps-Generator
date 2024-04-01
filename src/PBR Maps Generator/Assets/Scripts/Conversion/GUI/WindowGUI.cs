using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class WindowGUI : MonoBehaviour
{
    List<string> inputMapLabels, commonMapLabels, mrMapLabels, sgMapLabels;
    [SerializeField] TextMeshProUGUI[] mapLabelFields;
    [HideInInspector] public InputMaps currentInput = InputMaps.BASE;
    private void Start()
    {
        CreateMapNameStrings();
        AssignLabels(currentInput);
    }
    void AssignLabels(InputMaps _currentInput)
    {        
        mapLabelFields[0].text = EnumString(_currentInput.ToString());

        for (int i = 0; i < commonMapLabels.Count; i++)
            mapLabelFields[i + 1].text = commonMapLabels[i];

        switch (_currentInput)
        {
            case InputMaps.BASE:
                for (int i = 0; i < mrMapLabels.Count; i++)
                    mapLabelFields[i + 4].text = mrMapLabels[i];
                break;
            case InputMaps.DIFFUSE:
                for (int i = 0; i < sgMapLabels.Count; i++)
                    mapLabelFields[i + 4].text = sgMapLabels[i];
                break;
        }
    }
    void CreateMapNameStrings()
    {
        inputMapLabels = new List<string>();
        foreach (InputMaps map in System.Enum.GetValues(typeof(InputMaps)))
            inputMapLabels.Add(EnumString(map.ToString()));

        commonMapLabels = new List<string>();
        foreach (CommonMaps map in System.Enum.GetValues(typeof(CommonMaps)))
                commonMapLabels.Add(EnumString(map.ToString()));

        mrMapLabels = new List<string>();
        foreach (MRMaps map in System.Enum.GetValues(typeof(MRMaps)))
            mrMapLabels.Add(EnumString(map.ToString()));

        sgMapLabels = new List<string>();
        foreach (SGMaps map in System.Enum.GetValues(typeof(SGMaps)))
            sgMapLabels.Add(EnumString(map.ToString()));
    }

    public void OnMRToggleClicked() => AssignLabels(currentInput = InputMaps.BASE);
    public void OnSGToggleClicked() => AssignLabels(currentInput = InputMaps.DIFFUSE);
    string EnumString(string enumName) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enumName.ToLower().Replace("_", " "));
}
