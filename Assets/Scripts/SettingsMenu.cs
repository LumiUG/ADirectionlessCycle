using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Dropdown resolutionDropdown;

    private Resolution[] resolutions;

    private void Start()
    {
        // Resolution dropdown menu
        resolutions = Screen.resolutions;
        List<string> options = new();
        resolutionDropdown.ClearOptions();

        // Populate and update dropdown
        foreach (Resolution resolution in resolutions) { options.Add($"{resolution.width}x{resolution.height}"); }
        resolutionDropdown.AddOptions(options);
    }

    // Changes the game resolution
    public void ChangeResolution(int resolution)
    {
        Screen.SetResolution(resolutions[resolution].width, resolutions[resolution].height, Screen.fullScreen);
        UI.Instance.global.SendMessage($"New resolution set to {resolutions[resolution].width}x{resolutions[resolution].height}.", 2f);
    }

    // Toggle fullscreen
    public void ToggleFullscreen(bool toggle) { Screen.fullScreen = toggle; }
}
