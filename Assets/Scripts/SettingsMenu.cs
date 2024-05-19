using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Dropdown resolutionDropdown;
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

        // Update sliders (change from GameManager in the future)
        // masterSlider.value = 1;
        // SFXSlider.value = 1;

        // Version text
        version.text = $"Running v{Application.version}";
    }

    // Changes the game resolution
    public void ChangeResolution(int res)
    {
        int[] changeTo = resolutions[res].Split("x").ToList().ConvertAll(res => { return int.Parse(res); }).ToArray();
        Screen.SetResolution(changeTo[0], changeTo[1], Screen.fullScreen);
        UI.Instance.global.SendMessage($"New resolution set.", 2f);
    }

    // Toggle fullscreen (disabled for now)
    public void ToggleFullscreen(bool toggle)
    {
        Screen.fullScreen = toggle;
    }
}
