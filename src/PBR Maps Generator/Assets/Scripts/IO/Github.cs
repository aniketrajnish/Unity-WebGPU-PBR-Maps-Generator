using UnityEngine;
using UnityEngine.UI;

public class Github : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenGithubLink);
    }
    void OpenGithubLink() => Application.OpenURL("https://github.com/aniketrajnish/PBR-Maps-Generator");
}
