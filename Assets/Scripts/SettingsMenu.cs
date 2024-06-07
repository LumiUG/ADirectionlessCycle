using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Dropdown resolutionDropdown;
    public Toggle settingsToggle;
    public Toggle repeatInputToggle;
    public Slider masterSlider;
    public Slider SFXSlider;
    public Text version;

    private readonly List<string> resolutions = new();

    private void Start()
    {
        // Resolution dropdown menu
        resolutionDropdown.ClearOptions();

        // Populate and update dropdown
        resolutions.Add("1920x1080");
        resolutions.Add("1600x900");
        resolutions.Add("1366x768");
        resolutions.Add("1280x720");
        resolutions.Add("1152x648");
        resolutions.Add("1024x576");
        resolutionDropdown.AddOptions(resolutions);

        // Select current resolution
        int currentIndex = resolutions.FindIndex(res => { return res == $"{Screen.width}x{Screen.height}"; });
        if (currentIndex != -1) resolutionDropdown.value = currentIndex;

        // Update UI (change from GameManager in the future)
        EventSystem.current.SetSelectedGameObject(resolutionDropdown.gameObject);
        settingsToggle.isOn = Screen.fullScreen;
        repeatInputToggle.isOn = GameManager.save.preferences.repeatInput;

        masterSlider.value = GameManager.save.preferences.masterVolume;
        SFXSlider.value = GameManager.save.preferences.SFXVolume;

        // Version text
        version.text = $"Running v{Application.version}";
    }

    // Changes the game resolution
    public void ChangeResolution(int res)
    {
        int[] changeTo = resolutions[res].Split("x").ToList().ConvertAll(res => { return int.Parse(res); }).ToArray();
        Screen.SetResolution(changeTo[0], changeTo[1], Screen.fullScreen);
    }

    // Toggle fullscreen
    public void ToggleFullscreen(bool toggle)
    {
        Screen.fullScreen = toggle;
    }

        // Toggle input repeating
    public void ToggleRepeatingInput(bool toggle)
    {
        GameManager.save.preferences.repeatInput = toggle;
    }
}
