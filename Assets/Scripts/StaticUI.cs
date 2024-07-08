using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StaticUI : MonoBehaviour
{
    void Start()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu") return;
        EventSystem.current.SetSelectedGameObject(GameObject.Find("Play Button"));
    }

    // Change scene to the level editor
    public void StaticGoLevelEditor() { if (UI.Instance) UI.Instance.GoLevelEditor(); }

    // Change scene to the world map / real hub
    public void StaticGoWorldMap() { if (UI.Instance) UI.Instance.GoWorldMap(); }

    // Change scenes
    public void StaticChangeScene(string sceneName)
    {
        if (UI.Instance) UI.Instance.ChangeScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    // Exit application
    public void StaticExitApplication() { Application.Quit(); }

    // Master Slider
    public void StaticUpdateMasterSlider(float value)
    {
        if (AudioManager.Instance) AudioManager.Instance.SetMasterVolume(value);
        GameManager.save.preferences.masterVolume = value;
    }

    // SFX Slider
    public void StaticUpdateSFXSlider(float value)
    {
        GameManager.save.preferences.SFXVolume = value;
    }
}
