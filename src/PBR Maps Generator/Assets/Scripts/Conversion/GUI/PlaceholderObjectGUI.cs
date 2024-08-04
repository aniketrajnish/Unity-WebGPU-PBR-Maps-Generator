using System;
using UnityEngine;

public class PlaceholderObjectGUI : MonoBehaviour
{
    [SerializeField] float rotSpeed = 100f, dragSpeed = 100f, scaleSpeed = 100f;
    [SerializeField] Transform sun;
    [SerializeField] Mesh[] meshes;
    [SerializeField] MeshFilter meshHolder;
    [SerializeField] public Material objectMaterial;
    [SerializeField] GameObject instructionsPanel;

    int meshIndex = 0;
    float minScale = 0.5f, maxScale = 1.5f, timeCheck;
    Vector3 randomDir, dragOrigin;
    bool isDraggingObject = false, isDraggingSun = false;    
    private void Start() => AssignRandDir();
    private void Update()
    {
        CheckDrag();
        ObjectRotControls();
        ObjectScaleControls();
        SunRotControls();
    }
    void AssignRandDir() => randomDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 
                                                    UnityEngine.Random.Range(-1f, 1f), 
                                                    UnityEngine.Random.Range(-1f, 1f));
    void CheckDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            isDraggingObject = true;
        }
        if (Input.GetMouseButton(0))
            timeCheck += Time.deltaTime;

        if (Input.GetMouseButtonUp(0))
        {
            timeCheck = 0;
            isDraggingObject = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = Input.mousePosition;
            isDraggingSun = true;
        }
        if (Input.GetMouseButtonUp(1))
            isDraggingSun = false;
    }
    void ObjectRotControls()
    {
        if (isDraggingObject)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;

            Quaternion rotX = Quaternion.AngleAxis(delta.y * dragSpeed * Time.deltaTime, Vector3.forward);
            Quaternion rotY = Quaternion.AngleAxis(-delta.x * dragSpeed * Time.deltaTime, Vector3.up);

            transform.rotation = rotX * rotY * transform.rotation;
        }
        else
            transform.Rotate(randomDir * rotSpeed * Time.deltaTime);
    }
    void ObjectScaleControls()
    {
        Vector3 objectScale = transform.localScale + Vector3.one * Input.mouseScrollDelta.y * scaleSpeed * Time.deltaTime;

        objectScale = new Vector3(Mathf.Clamp(objectScale.x, minScale, maxScale),
            Mathf.Clamp(objectScale.y, minScale, maxScale),
            Mathf.Clamp(objectScale.z, minScale, maxScale));

        transform.localScale = objectScale;
    }
    void SunRotControls()
    {
        if (isDraggingSun)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;

            Quaternion rotX = Quaternion.AngleAxis(delta.y * dragSpeed * Time.deltaTime, Vector3.forward);
            Quaternion rotY = Quaternion.AngleAxis(-delta.x * dragSpeed * Time.deltaTime, Vector3.up);

            sun.transform.rotation = rotX * rotY * sun.transform.rotation;
        }
    }
    public void SwitchMesh()
    {
        if (timeCheck > .25f)
            return;

        meshIndex = (meshIndex + 1) % meshes.Length;
        meshHolder.mesh = meshes[meshIndex];

        if (meshIndex != 0)
        {
            minScale = .25f;
            maxScale = 1;

            transform.localScale = transform.localScale * .5f;
        }
        else
        {
            minScale = .5f;
            maxScale = 1.5f;

            transform.localScale = transform.localScale * 2f;
        }
    }
    public void EnableDisableInstructionsPanel() => instructionsPanel.SetActive(!instructionsPanel.activeSelf);
#nullable enable
    public void UpdateMaterialTextures(Texture2D? baseMap = null, Texture2D? diffuseMap = null, Texture2D? heightMap = null, Texture2D? normalMap = null, Texture2D? aoMap = null, Texture2D? metallicMap = null, Texture2D? roughnessMap = null, Texture2D? specularMap = null, Texture2D? glossinessMap = null)
    {
        if (baseMap != null) objectMaterial.SetTexture("_MainTex", baseMap);
        if (diffuseMap != null) objectMaterial.SetTexture("_MainTex", diffuseMap);
        if (heightMap != null)
        {
            objectMaterial.SetTexture("_ParallaxMap", heightMap);
            objectMaterial.SetFloat("_Parallax", 0.08f);
        }
        if (normalMap != null)
        {
            objectMaterial.SetTexture("_BumpMap", normalMap);
            objectMaterial.SetFloat("_BumpScale", 1.0f);
        }
        if (aoMap != null) objectMaterial.SetTexture("_OcclusionMap", aoMap);

        if (specularMap != null && glossinessMap != null)
        {
            objectMaterial.shader = Shader.Find("Standard (Specular setup)");
            objectMaterial.SetTexture("_SpecGlossMap", specularMap);
            objectMaterial.SetColor("_SpecColor", specularMap.GetPixel(0, 0));
            objectMaterial.SetFloat("_Glossiness", glossinessMap.GetPixel(0, 0).r);

        }
        else if (metallicMap != null && roughnessMap != null)
        {
            objectMaterial.shader = Shader.Find("Standard");
            objectMaterial.SetTexture("_MetallicGlossMap", metallicMap);
            objectMaterial.SetFloat("_Metallic", 1.0f);
            objectMaterial.SetFloat("_Glossiness", roughnessMap.GetPixel(0, 0).r);
        }

        objectMaterial.shader = objectMaterial.shader;
    }
#nullable disable
}
