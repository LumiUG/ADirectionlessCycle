using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [HideInInspector] public static SettingsMenu I;
    [HideInInspector] public int menuIndex = 0;
    public Dropdown resolutionDropdown;
    public Dropdown outlineDropdown;
    public Toggle settingsToggle;
    public Toggle missingToggle;
    public Toggle repeatInputToggle;
    public Toggle moveToggle;
    public Toggle timerToggle;
    public Toggle restartToggle;
    public Slider masterSlider;
    public Slider SFXSlider;
    public Text cycleControls;
    public GameObject eraseObject;
    public Text eraseCounter;
    public List<GameObject> menus = new();
    public List<GameObject> buttons = new();

    private readonly List<string> resolutions = new();
    private readonly List<string> outlines = new();
    private int eraseClicks = 3;

    private void Awake() { I = this; }

    private void Start()
    {
        UI.I.selectors.ChangeSelected(buttons[0], true);
        menuIndex = 0;

        // Dropdown menus
        resolutionDropdown.ClearOptions();
        outlineDropdown.ClearOptions();

        // Populate and update dropdowns
        resolutions.Add("1920x1080");
        resolutions.Add("1600x900"); // looks odd
        resolutions.Add("1366x768");
        resolutions.Add("1280x720");
        // resolutions.Add("1152x648"); // looks odd
        // resolutions.Add("1024x576"); // looks odd
        resolutionDropdown.AddOptions(resolutions);

        outlines.Add("Dotted");
        outlines.Add("Full");
        outlines.Add("NONE");
        outlineDropdown.AddOptions(outlines);

        // Select current dropdown values
        int currentIndex = resolutions.FindIndex(res => { return res == $"{Screen.width}x{Screen.height}"; });
        if (currentIndex != -1) resolutionDropdown.value = currentIndex;

        currentIndex = outlines.FindIndex(outline => { return outline == GameManager.save.preferences.outlineType; });
        if (currentIndex != -1) outlineDropdown.value = currentIndex;

        // Update UI
        if (GameManager.save.game.mechanics.hasSwapUpgrade) cycleControls.text = "Cycle";
        settingsToggle.isOn = Screen.fullScreen;
        missingToggle.isOn = GameManager.save.preferences.missingHighlighter;
        repeatInputToggle.isOn = GameManager.save.preferences.repeatInput;
        restartToggle.isOn = GameManager.save.preferences.forceConfirmRestart;
        moveToggle.isOn = GameManager.save.preferences.showMoves;
        timerToggle.isOn = GameManager.save.preferences.showTimer;

        masterSlider.value = GameManager.save.preferences.masterVolume;
        SFXSlider.value = GameManager.save.preferences.SFXVolume;
    }

    // Sometimes when interacting with a dropdown menu
    // Unity destroys the reference to the last object interacted with
    // and doesnt fix it. Bravo.
    public void FixedUpdate()
    {
        if (!EventSystem.current) return;
        if (EventSystem.current.currentSelectedGameObject == null) UI.I.selectors.ChangeSelected(menus[menuIndex].transform.Find("Back Button").gameObject, true);
    }

    // Changes the game resolution
    public void ChangeResolution(int res)
    {
        int[] changeTo = resolutions[res].Split("x").ToList().ConvertAll(res => { return int.Parse(res); }).ToArray();
        Screen.SetResolution(changeTo[0], changeTo[1], Screen.fullScreen);
    }


    // Sets the new tile's outline
    public void ChangeOutline(int index)
    {
        GameManager.save.preferences.outlineType = outlines[index];
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

    // Toggle input repeating
    public void ToggleConfirmRestart(bool toggle)
    {
        GameManager.save.preferences.forceConfirmRestart = toggle;
    }

    public void ToggleTimer(bool toggle)
    {
        GameManager.save.preferences.showTimer = toggle;
    }

    public void ToggleMoves(bool toggle)
    {
        GameManager.save.preferences.showMoves = toggle;
    }

    public void ToggleHighlighter(bool toggle)
    {
        GameManager.save.preferences.missingHighlighter = toggle;
    }

    public void EraseSave()
    {
        eraseClicks--;
        eraseCounter.text = $"({eraseClicks})";

        if (eraseClicks <= 0)
        {
            eraseClicks = 3;
            eraseCounter.text = $"Savefile ERASED!";

            GameManager.I.DeleteSave();
            GameManager.I.CreateSave(true);

            ResetSetting(0);
            ResetSetting(1);
            ResetSetting(2);
            ResetSetting(3);
        }
    }

    // Resets a setting to its default values (hardcoded, here)
    public void ResetSetting(int index)
    {
        switch (index)
        {
            case 0:
                GameManager.save.preferences.forceConfirmRestart = true;
                GameManager.save.preferences.repeatInput = true;
                GameManager.save.preferences.showTimer = false;
                GameManager.save.preferences.showMoves = true;
                repeatInputToggle.isOn = true;
                restartToggle.isOn = true;
                timerToggle.isOn = false;
                moveToggle.isOn = true;
                break;
            case 1:
                ToggleFullscreen(true);
                ChangeResolution(0);
                GameManager.save.preferences.missingHighlighter = false;
                resolutionDropdown.value = 0;
                outlineDropdown.value = 0;
                settingsToggle.isOn = true;
                missingToggle.isOn = false;
                break;
            case 2:
                GameManager.save.preferences.masterVolume = 0.6f;
                GameManager.save.preferences.SFXVolume = 0.80f;
                masterSlider.value = 0.6f;
                SFXSlider.value = 0.80f;
                break;
            case 3:
                break;
        }
    }

    // Toggles one of the settings menus
    public void ToggleMenu(int index)
    {
        if (menuIndex == index || index >= menus.Count || index < 0) return;
        foreach (GameObject menu in menus) { menu.SetActive(false); }

        if (buttons[index] != EventSystem.current.currentSelectedGameObject) UI.I.selectors.ChangeSelected(buttons[index]);
        eraseObject.SetActive(menus[index].name == "Gameplay Menu");
        menus[index].SetActive(true);
        menuIndex = index;
    }
}