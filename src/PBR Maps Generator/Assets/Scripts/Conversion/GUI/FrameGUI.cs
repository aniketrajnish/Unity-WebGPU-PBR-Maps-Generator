using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;
public class FrameGUI : MonoBehaviour
{
    [SerializeField] List<GameObject> mapFrameGOs;
    [SerializeField] UploadDownload io;
    public List<MapFrame> mapFrames;
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
            Image mapImage = go.transform.GetComponentsInChildren<Image>()[2];
            TextMeshProUGUI mapLabelField = go.transform.GetComponentInChildren<TextMeshProUGUI>();
            Button downloadBtn = go.transform.GetComponentInChildren<Button>();

            mapFrames.Add(new MapFrame(mapImage, mapLabelField, downloadBtn));

            if (mapLabelField.text.Trim() != "Base")
                downloadBtn.onClick.AddListener(() => io.OnDownloadBtnClick(io.inputMapFileName 
                    + "_" + mapLabelField.text.Trim(), io.inputMapExtension, 
                    FixTexture.UncompressAndExposeTexture(mapImage.sprite.texture)));                
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
    public void UpdateTextures()
    {
        Texture2D inputMap = mapFrames[0].mapImage.sprite.texture;

        ConvertTextureAndUpdateFrame(inputMap, HeightMap.ConvertToHeightMap, 1);
        ConvertTextureAndUpdateFrame(inputMap, NormalMap.ConvertToNormalMap, 2);
        ConvertTextureAndUpdateFrame(inputMap, AOMap.ConvertToAOMap, 3);

        switch (currentInput)
        {
            case InputMaps.BASE:
                ConvertTextureAndUpdateFrame(inputMap, MetallicMap.ConvertToMetallicMap, 4);
                ConvertTextureAndUpdateFrame(inputMap, RoughnessMap.ConvertToRoughnessMap, 5);
                break;
            case InputMaps.DIFFUSE:
                ConvertTextureAndUpdateFrame(inputMap, SpecularMap.ConvertToSpecularMap, 4);
                ConvertTextureAndUpdateFrame(inputMap, GlossinessMap.ConvertToGlossinessMap, 5);
                break;
        }
    }
    private void ConvertTextureAndUpdateFrame(Texture2D inputTexture, Func<Texture2D, Texture2D> conversionAlgorithm, int frameIndex)
    {
        Texture2D outputTexture = conversionAlgorithm(inputTexture);
        mapFrames[frameIndex].mapImage.sprite = Sprite.Create(outputTexture, new Rect(0, 0, outputTexture.width, outputTexture.height), new Vector2(0.5f, 0.5f));
    }
    public void OnMRToggleClicked() => AssignLabels(currentInput = InputMaps.BASE);
    public void OnSGToggleClicked() => AssignLabels(currentInput = InputMaps.DIFFUSE);
    string EnumString(string enumName) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enumName.ToLower().Replace("_", " "));
}
