using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using static TransitionManager.Transitions;
using static GameTile;
using ADC.Localization;

public sealed class Actions : MonoBehaviour
{
    public bool Initialized = false;

    private void Awake() => Initialized = true;

    // Level-Related Actions //
    public static void LoadLevel(string name)
    {
        var save = GameManager.save.game.levels.Find(level => level.levelID == name);

        // Loads the level (catching exceptions)
        try {
            LevelManager.I.LoadLevel(name, SceneManager.GetActiveScene().name == "Custom Levels");
        } catch (Exception) {
            UI.I.global.SendMessage("An error ocurred while loading!", 10f);
            UI.I.ChangeScene("Main Menu", false);
            LevelManager.I.ClearLevel();
            return;
        }

        LevelManager.I.RefreshGameVars();
        LevelManager.I.RefreshGameUI();

        // Preload screen
        TransitionManager.I.ChangeTransition(Triangle);
        UI.I.pause.title.text = LevelManager.I.currentLevel.levelName;
        if (!LevelManager.I.currentLevel.hideUI) UI.I.preload.PreparePreloadScreen(save);
        else {
            UI.I.ChangeScene("Game", false);
            TransitionManager.I.TransitionOut<string>();
        }
    }

    public static void RemixCondition(string remixID)
    {
        var save = GameManager.save.game.levels.Find(level => level.levelID == remixID);
        
        // Loads the level (Load internal level first, if it fails, load external)
        LevelManager.I.RefreshGameVars(); // necessary.
        if (!LevelManager.I.LoadLevel(remixID)) LevelManager.I.LoadLevel(remixID, true);
        if (LevelManager.I.currentLevelID == "W2/2-12") LevelManager.I.RefreshGameUI();

        // Preload screen
        TransitionManager.I.ChangeTransition(Unknown);
        if (!LevelManager.I.currentLevel.hideUI) UI.I.preload.PreparePreloadScreen(save);
        else TransitionManager.I.TransitionOut<string>();
    }

    public static void Restart(string _)
    {            
        LevelManager.I.ReloadLevel();
        LevelManager.I.RefreshGameVars();
        LevelManager.I.MoveTilemaps(LevelManager.I.originalPosition, true);
        UI.I.ingame.SetCycleIcon(ObjectTypes.Hexagon);
        UI.I.ingame.trialStatus.gameObject.SetActive(GameManager.I.isDoingTrial);
        if (GameManager.I.isDoingTrial) UI.I.ingame.trialCross.gameObject.SetActive(true);
        TransitionManager.I.TransitionOut<string>(Swipe);
    }


    // UI Actions //
    public static void ChangeScene(string sceneName)
    {
        // Load the scene
        SceneManager.LoadScene(sceneName);
    }

    public static void GoLevelEditor(string _)
    {
        if (SceneManager.GetActiveScene().name == "Game") LevelManager.I.ReloadLevel(true, true);
        else if (!LevelManager.I.LoadLevel("EditorSession", true))
        {
            // Create EditorSession if the file does not exist.
            LevelManager.I.SaveLevel(LevelManager.I.currentLevel.levelName, LevelManager.I.levelEditorName);
            LevelManager.I.LoadLevel("EditorSession", true);
        } else {
            LevelManager.I.ReloadLevel(true, true); // reload for custom sprites (very stupid!)
        }

        // Rich presence
        GameManager.I.SetPresence("editorlevel", LevelManager.I.currentLevel.levelName);
        GameManager.I.SetPresence("steam_display", "#Editor");
        GameManager.I.UpdateActivity($"Editing a level: {LevelManager.I.currentLevel.levelName}");

        LevelManager.I.RefreshGameVars();
        UI.I.ChangeScene("Level Editor", false);
        UI.I.ClearUI();
    }
    public static void GoNextLevel(string _)
    {
        var save = GameManager.save.game.levels.Find(level => level.levelID == LevelManager.I.currentLevel.nextLevel);

        // Loads the level (Load internal level first, if it fails, load external)
        LevelManager.I.RefreshGameVars();
        if (!LevelManager.I.LoadLevel(LevelManager.I.currentLevel.nextLevel)) LevelManager.I.LoadLevel(LevelManager.I.currentLevel.nextLevel, true);
        LevelManager.I.RefreshGameUI();

        // Preload screen
        TransitionManager.I.ChangeTransition(Triangle);
        if (!LevelManager.I.currentLevel.hideUI) UI.I.preload.PreparePreloadScreen(save);
        else TransitionManager.I.TransitionOut<string>();
    }

    public static void GoHintLevel(string hintLevelID)
    {
        // Loads the level
        LevelManager.I.RefreshGameVars();
        LevelManager.I.LoadLevel(hintLevelID);
        LevelManager.I.RefreshGameUI();

        // transition out
        TransitionManager.I.ChangeTransition(Triangle);
        TransitionManager.I.TransitionOut<string>();
    }

    public static void GoScene(string scene)
    {
        if (scene == "Main Menu") GameManager.I.lastSelectedWorld = 0;
        LevelManager.I.ClearLevel();
        LevelManager.I.hasWon = false;
        GameManager.I.isEditing = false;
        LevelManager.I.currentLevel = null;
        UI.I.ClearUI();

        UI.I.ChangeScene(scene, false);
    }
    public static void UIRestartLevel(string _)
    {
        // Hint popup
        // if (!GameManager.save.game.seenHintPopup)
        // {
        //     InputManager.I.restartCount++;
        //     if (InputManager.I.restartCount >= 5)
        //     {
        //         UI.I.popup.SetPopup("You seem stuck, need a hint? Press the lightbulb on the pause menu!");
        //         GameManager.save.game.seenHintPopup = true;
        //     }
        // }

        LevelManager.I.RefreshGameUI();
        Restart(null);
    }

    public static void ReturnHub(string _)
    {
        LevelManager.I.ClearLevel();
        LevelManager.I.hasWon = false;
        GameManager.I.isEditing = false;
        LevelManager.I.currentLevel = null;
        UI.I.ClearUI();
        UI.I.ChangeScene("Hub");
    }


    // Misc Actions //
    public static void RefreshCustomLevels(string filter)
    {
        // Clear all levels and re-load everything
        foreach (Transform level in CustomLevels.I.holder.transform) { Destroy(level.gameObject); }
        CustomLevels.I.holder.anchoredPosition = new Vector2(CustomLevels.I.holder.anchoredPosition.x, -540);
        CustomLevels.I.LoadCustomLevels(filter);
        TransitionManager.I.TransitionOut<string>(Refresh);
    }

    public static void DiveIn(string count)
    {
        TransitionManager.Transitions[] effects = { Dive, Unknown, Dive, Crossfade };
        int.TryParse(count, out int numberCount);

        if (count == "5")
        {
            TransitionManager.I.TransitionIn(Triangle, LoadLevel, "VOID/Entry");
            LevelManager.I.voidedCutscene = false;
            return;
        }

        TransitionManager.I.TransitionIn(effects[numberCount - 1], DiveOut, count);
    }

    public static void ExtraDiveIn(string count)
    {
        TransitionManager.Transitions[] effects = { Dive, Dive };
        int.TryParse(count, out int numberCount);

        if (count == "7")
        {
            TransitionManager.I.TransitionIn(Dive, LoadLevel, "VOID/Right");
            LevelManager.I.voidedCutscene = false;
            return;
        }

        TransitionManager.I.TransitionIn(effects[numberCount - 5], DiveOut, count);
    }

    public static void DiveOut(string count)
    {
        LevelManager.I.LoadLevel($"VOID/Dive/{count}");

        TransitionManager.Transitions[] effects = { Dive, Unknown, Dive, Crossfade };
        int.TryParse(count, out int numberCount);

        if (numberCount >= 5) TransitionManager.I.TransitionOut(Dive, ExtraDiveIn, $"{numberCount + 1}");
        else TransitionManager.I.TransitionOut(effects[numberCount - 1], DiveIn, $"{numberCount + 1}");
    }

    public static void SetLocale(string count)
    {
        int.TryParse(count, out int numberCount);
        LocalizationSettings.SelectedLocale = Localization.GetLocale(numberCount);

        TransitionManager.I.TransitionOut<string>(Reveal);
    }
}