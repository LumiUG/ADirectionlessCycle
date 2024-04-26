using UnityEngine;
using UnityEngine.SceneManagement;

public class StaticUI : MonoBehaviour
{
    // Change scenes
    public void StaticChangeScene(string sceneName) { SceneManager.LoadScene(sceneName); }

    // Exit application
    public void StaticExitApplication() { Application.Quit(); }

    // Master Slider (reroute to UI script)
    public void StaticUpdateMasterSlider(float value) { Debug.Log($"Master: {value}"); }

    // SFX Slider (reroute to UI script)
    public void StaticUpdateSFXSlider(float value) { Debug.Log($"SFX: {value}"); }
}
