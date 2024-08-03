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
    [SerializeField] PlaceholderObjectGUI pog;
    [SerializeField] UX ux;
    public List<MapFrame> mapFrames;
    Dictionary<string, InputMaps> inputMapLabels;
    Dictionary<string, CommonMaps> commonMapLabels;
    Dictionary<string, MRMaps> mrMapLabels;
    Dictionary<string, SGMaps> sgMapLabels;
    Dictionary<string, Texture2D> generatedMaps = new Dictionary<string, Texture2D>();
    [HideInInspector] public InputMaps currentInput = InputMaps.BASE;
    private void Awake() => io.OnImageLoaded += UpdateTextures;
    private void OnDestroy() => io.OnImageLoaded -= UpdateTextures;
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
            Button btn = go.transform.GetComponentInChildren<Button>();

            mapFrames.Add(new MapFrame(mapImage, mapLabelField, btn));

            if (mapLabelField.text.Trim() != "Base")
                btn.onClick.AddListener(() => io.OnDownloadBtnClick(io.uploadImgFileName
                    + "_" + mapLabelField.text.Trim(), io.uploadImgExtension,
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
    public void UpdateTextures(Texture2D inputMap)
    {
        generatedMaps["Height"] = ConvertTextureAndUpdateFrame(inputMap, HeightMap.GPUConvertToHeightMap, 1);
        generatedMaps["Normal"] = ConvertTextureAndUpdateFrame(inputMap, NormalMap.GPUConvertToNormalMap, 2);
        generatedMaps["AO"] = ConvertTextureAndUpdateFrame(inputMap, AOMap.GPUConvertToAOMap, 3);

        switch (currentInput)
        {
            case InputMaps.BASE:

                generatedMaps["Metallic"] = ConvertTextureAndUpdateFrame(inputMap, MetallicMap.GPUConvertToMetallicMap, 4);
                generatedMaps["Roughness"] = ConvertTextureAndUpdateFrame(inputMap, RoughnessMap.GPUConvertToRoughnessMap, 5);

                generatedMaps["Specular"] = ConvertTextureAndUpdateFrame(inputMap, SpecularMap.GPUConvertToSpecularMap);
                generatedMaps["Glossiness"] = ConvertTextureAndUpdateFrame(inputMap, GlossinessMap.GPUConvertToGlossinessMap);

                pog.UpdateMaterialTextures(baseMap: inputMap, 
                    heightMap: generatedMaps["Height"], 
                    normalMap: generatedMaps["Normal"], 
                    aoMap: generatedMaps["AO"], 
                    metallicMap: generatedMaps["Metallic"], 
                    roughnessMap: generatedMaps["Roughness"]);

                break;

            case InputMaps.DIFFUSE:
                generatedMaps["Specular"] = ConvertTextureAndUpdateFrame(inputMap, SpecularMap.GPUConvertToSpecularMap, 4);
                generatedMaps["Glossiness"] = ConvertTextureAndUpdateFrame(inputMap, GlossinessMap.GPUConvertToGlossinessMap, 5);

                generatedMaps["Metallic"] = ConvertTextureAndUpdateFrame(inputMap, MetallicMap.GPUConvertToMetallicMap);
                generatedMaps["Roughness"] = ConvertTextureAndUpdateFrame(inputMap, RoughnessMap.GPUConvertToRoughnessMap);

                pog.UpdateMaterialTextures(diffuseMap: inputMap, 
                    heightMap: generatedMaps["Height"], 
                    normalMap: generatedMaps["Normal"], 
                    aoMap: generatedMaps["AO"], 
                    specularMap: generatedMaps["Specular"], 
                    glossinessMap: generatedMaps["Glossiness"]);

                break;
        }
        ux.OnImageUploaded();
    }
    private Texture2D ConvertTextureAndUpdateFrame(Texture2D inputTexture, Func<Texture2D, Texture2D> conversionAlgorithm) => conversionAlgorithm(inputTexture);
    private Texture2D ConvertTextureAndUpdateFrame(Texture2D inputTexture, Func<Texture2D, Texture2D> conversionAlgorithm, int frameIndex)
    {
        Texture2D outputTexture = conversionAlgorithm(inputTexture);
        if (mapFrames[frameIndex].mapImage.sprite.texture != outputTexture)
            mapFrames[frameIndex].mapImage.sprite = Sprite.Create(outputTexture,
                    new Rect(0, 0, outputTexture.width, outputTexture.height), new Vector2(0.5f, 0.5f));
        return outputTexture;
    }
    public void DownloadAllMaps()
    {
        UpdateTextures(io.uploadImgHolder.sprite.texture);

        foreach (var map in generatedMaps)
            io.OnDownloadBtnClick(map.Key + "_" + io.uploadImgFileName, io.uploadImgExtension, map.Value);
    }
    public void OnMRToggleClicked()
    {
        AssignLabels(currentInput = InputMaps.BASE);

        if (generatedMaps.ContainsKey("Metallic") && generatedMaps.ContainsKey("Roughness"))
        {
            mapFrames[4].mapImage.sprite = Sprite.Create(generatedMaps["Metallic"],
                new Rect(0, 0, generatedMaps["Metallic"].width, generatedMaps["Metallic"].height), new Vector2(0.5f, 0.5f));
            mapFrames[5].mapImage.sprite = Sprite.Create(generatedMaps["Roughness"],
                    new Rect(0, 0, generatedMaps["Roughness"].width, generatedMaps["Roughness"].height), new Vector2(0.5f, 0.5f));

            pog.UpdateMaterialTextures(metallicMap: generatedMaps["Metallic"], roughnessMap: generatedMaps["Roughness"]);
        }
    }
    public void OnSGToggleClicked()
    {
        AssignLabels(currentInput = InputMaps.DIFFUSE);

        if (generatedMaps.ContainsKey("Specular") && generatedMaps.ContainsKey("Glossiness"))
        {
            mapFrames[4].mapImage.sprite = Sprite.Create(generatedMaps["Specular"],
                           new Rect(0, 0, generatedMaps["Specular"].width, generatedMaps["Specular"].height), new Vector2(0.5f, 0.5f));
            mapFrames[5].mapImage.sprite = Sprite.Create(generatedMaps["Glossiness"],
                               new Rect(0, 0, generatedMaps["Glossiness"].width, generatedMaps["Glossiness"].height), new Vector2(0.5f, 0.5f));

            pog.UpdateMaterialTextures(specularMap: generatedMaps["Specular"], glossinessMap: generatedMaps["Glossiness"]);
        }

    }
    string EnumString(string enumName) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enumName.ToLower().Replace("_", " "));
}
