using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class FrameGUI : MonoBehaviour
{
    [SerializeField] List<GameObject> mapFrameGOs;
    [SerializeField] UploadDownload io;
    [SerializeField] PlaceholderObjectGUI pog;
    [SerializeField] UX ux;
    [SerializeField] Toggle mrToggle, sgToggle;
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
            {
                string mapName = mapLabelField.text.Trim();

                btn.onClick.AddListener(() =>
                {
                    if (generatedMaps.ContainsKey(mapName))
                    {
                        io.OnDownloadBtnClick(
                            io.uploadImgFileName + "_" + mapName,
                            io.uploadImgExtension,
                            generatedMaps[mapName]
                        );
                    }
                    else
                    {
                        Debug.LogWarning($"Map {mapName} not found in generatedMaps dictionary.");
                    }
                });
            }
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
        int mapsToGenerate = 7;
        int mapsGenerated = 0;

        void CheckAllMapsGenerated()
        {
            mapsGenerated++;
            if (mapsGenerated == mapsToGenerate)
            {
                UpdateFramesBasedOnWorkflow();
                UpdateMaterialTextures();
                print("All maps generated");
                ux.OnImageUploaded();
            }
        }

        HeightMap.GPUConvertToHeightMap(inputMap, (heightMap) =>
        {
            if (heightMap != null)
            {
                generatedMaps["Height"] = heightMap;
                UpdateFrame(heightMap, 1);                
            }
            else
            {
                Debug.LogError("Failed to generate Height map");
            }
            CheckAllMapsGenerated();
        });

        NormalMap.GPUConvertToNormalMap(inputMap, (normalMap) =>
        {
            if (normalMap != null)
            {
                generatedMaps["Normal"] = normalMap;
                UpdateFrame(normalMap, 2);
            }
            else
            {
                Debug.LogError("Failed to generate Normal map");
            }
            CheckAllMapsGenerated();
        });       

        AOMap.GPUConvertToAOMap(inputMap, (aoMap) =>
        {
            if (aoMap != null)
            {
                generatedMaps["AO"] = aoMap;
                UpdateFrame(aoMap, 3);
            }
            else
            {
                Debug.LogError("Failed to generate AO map");
            }
            CheckAllMapsGenerated();
        });

        MetallicMap.GPUConvertToMetallicMap(inputMap, (metallicMap) =>
        {
            if (metallicMap != null)
            {
                generatedMaps["Metallic"] = metallicMap;
            }
            else
            {
                Debug.LogError("Failed to generate Metallic map");
            }
            CheckAllMapsGenerated();
        });

        RoughnessMap.GPUConvertToRoughnessMap(inputMap, (roughnessMap) =>
        {
            if (roughnessMap != null)
            {
                generatedMaps["Roughness"] = roughnessMap;                
            }
            else
            {
                Debug.LogError("Failed to generate Roughness map");
            }
            CheckAllMapsGenerated();
        });

        SpecularMap.GPUConvertToSpecularMap(inputMap, (specularMap) =>
        {
            if (specularMap != null)
            {
                generatedMaps["Specular"] = specularMap;
            }
            else
            {
                Debug.LogError("Failed to generate Specular map");
            }
            CheckAllMapsGenerated();
        });
        GlossinessMap.GPUConvertToGlossinessMap(inputMap, (glossinessMap) =>
        {
            if (glossinessMap != null)
            {
                generatedMaps["Glossiness"] = glossinessMap;
            }
            else
            {
                Debug.LogError("Failed to generate Glossiness map");
            }
            CheckAllMapsGenerated();
        });
    }

    private void UpdateFramesBasedOnWorkflow()
    {
        switch (currentInput)
        {
            case InputMaps.BASE:
                UpdateFrame(generatedMaps["Metallic"], 4);
                UpdateFrame(generatedMaps["Roughness"], 5);
                break;
            case InputMaps.DIFFUSE:
                UpdateFrame(generatedMaps["Specular"], 4);
                UpdateFrame(generatedMaps["Glossiness"], 5);
                break;
        }
    }

    private void UpdateFrame(Texture2D texture, int frameIndex)
    {
        if (texture == null) return;

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (mapFrames[frameIndex].mapImage.sprite.texture != texture)
            {
                mapFrames[frameIndex].mapImage.sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        });
    }

    private void UpdateMaterialTextures()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            switch (currentInput)
            {
                case InputMaps.BASE:
                    pog.UpdateMaterialTextures(
                        baseMap: io.uploadImgHolder.sprite.texture,
                        heightMap: generatedMaps.ContainsKey("Height") ? generatedMaps["Height"] : null,
                        normalMap: generatedMaps.ContainsKey("Normal") ? generatedMaps["Normal"] : null,
                        aoMap: generatedMaps.ContainsKey("AO") ? generatedMaps["AO"] : null,
                        metallicMap: generatedMaps.ContainsKey("Metallic") ? generatedMaps["Metallic"] : null,
                        roughnessMap: generatedMaps.ContainsKey("Roughness") ? generatedMaps["Roughness"] : null
                    );
                    break;

                case InputMaps.DIFFUSE:
                    pog.UpdateMaterialTextures(
                        diffuseMap: io.uploadImgHolder.sprite.texture,
                        heightMap: generatedMaps.ContainsKey("Height") ? generatedMaps["Height"] : null,
                        normalMap: generatedMaps.ContainsKey("Normal") ? generatedMaps["Normal"] : null,
                        aoMap: generatedMaps.ContainsKey("AO") ? generatedMaps["AO"] : null,
                        specularMap: generatedMaps.ContainsKey("Specular") ? generatedMaps["Specular"] : null,
                        glossinessMap: generatedMaps.ContainsKey("Glossiness") ? generatedMaps["Glossiness"] : null
                    );
                    break;
            }
        });
    }

    public void DownloadAllMaps()
    {
        foreach (var map in generatedMaps)
            io.OnDownloadBtnClick(map.Key + "_" + io.uploadImgFileName, io.uploadImgExtension, map.Value);
    }   
    public void onMRSGToggleClicked()
    {
        if (sgToggle.isOn && currentInput != InputMaps.DIFFUSE)
        {
            print("Switching to Diffuse workflow");
            AssignLabels(currentInput = InputMaps.DIFFUSE);
            UpdateFramesBasedOnWorkflow();
            UpdateMaterialTextures();
        }
        else if (mrToggle.isOn && currentInput != InputMaps.BASE)
        {
            print("Switching to Base workflow");
            AssignLabels(currentInput = InputMaps.BASE);
            UpdateFramesBasedOnWorkflow();
            UpdateMaterialTextures();
        }
    }

    string EnumString(string enumName) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enumName.ToLower().Replace("_", " "));
}