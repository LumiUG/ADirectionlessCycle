using UnityEngine;
using UnityEngine.SceneManagement;

public class UIEvents : MonoBehaviour
{
    // Change scenes
    public void ChangeScene(string sceneName) { SceneManager.LoadScene(sceneName); }

    // Exit application
    public void ExitApplication() { Application.Quit(); }

    // Master Slider
    public void UpdateMasterSlider(float value) { Debug.Log($"Master: {value}"); }

    // SFX Slider
    public void UpdateSFXSlider(float value) { Debug.Log($"SFX: {value}"); }
}
