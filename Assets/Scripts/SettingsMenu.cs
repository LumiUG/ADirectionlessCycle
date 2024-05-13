using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Dropdown resolutionDropdown;
    public Slider masterSlider;
    public Slider SFXSlider;
    public Text version;

    private Resolution[] resolutions;

    private void Start()
    {
        // Resolution dropdown menu
        resolutions = Screen.resolutions;
        List<string> options = new();
        resolutionDropdown.ClearOptions();

        // Populate and update dropdown
        options.Add("Default");
        options.Add("NATIVE");
        foreach (Resolution resolution in resolutions) { options.Add($"{resolution.width}x{resolution.height}"); }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.options.RemoveAt(0);

        // Update sliders (change from GameManager in the future)
        // masterSlider.value = 1;
        // SFXSlider.value = 1;

        // Version text
        version.text = $"Running v{Application.version}";
    }

    // Changes the game resolution
    public void ChangeResolution(int res)
    {
        // Native resolution
        if (res == 0)
        {
            Screen.SetResolution(1920, 1080, true);
            UI.Instance.global.camera.orthographicSize = 6;
            UI.Instance.global.SendMessage($"New resolution set to NATIVE.", 2f);
        }

        // Scaled screen (very buggy, unfinished)
        else
        {
            res--;
            Screen.SetResolution(resolutions[res].width, resolutions[res].height, true);
            UI.Instance.global.camera.orthographicSize = 22f * Screen.height / Screen.width * 0.5f;
            UI.Instance.global.SendMessage($"New resolution set to {resolutions[res].width}x{resolutions[res].height}.", 2f);
        }
    }

    // Toggle fullscreen (disabled for now)
    public void ToggleFullscreen(bool toggle)
    {
        //Screen.fullScreen = toggle;
    }
}
