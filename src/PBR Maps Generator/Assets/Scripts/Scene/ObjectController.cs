using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class ObjectController : MonoBehaviour
{
    [SerializeField] float rotSpeed = 100f, dragSpeed = 100f, scaleSpeed = 100f;
    [SerializeField] Transform sun;
    [SerializeField] Mesh[] meshes;
    [SerializeField] MeshFilter meshHolder;
    [SerializeField] GameObject instructionsPanel;

    int meshIndex = 0;
    float minScale = 0.5f, maxScale = 1.5f;
    Vector3 randomDir, dragOrigin;
    bool isDraggingObject = false, isDraggingSun = false;
    private void Start()
    {
        AssignRandDir();
    }
    private void Update()
    {
        CheckDrag();        
        ObjectRotControls();
        ObjectScaleControls();
        SunRotControls();
    }
    void AssignRandDir() => randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    void CheckDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            isDraggingObject = true;
        }
        if (Input.GetMouseButtonUp(0))
            isDraggingObject = false;

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
}
