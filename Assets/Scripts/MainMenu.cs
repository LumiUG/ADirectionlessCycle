using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Text version;

    private void Start()
    {
        // Version text
        version.text = $"v{Application.version}";
    }
}
