using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [HideInInspector] public static MainMenu I;
    public Text version;
    public Text debug;

    private void Start()
    {
        I = this; // No persistence!
        
        // Version text
        version.text = $"v{Application.version}";
    }

    // Play button event
    public void Play()
    {
        UI.Instance.ChangeScene("Hub");
    }
}
