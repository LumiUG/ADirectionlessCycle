using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TransitionManager.Transitions;

public class UI : MonoBehaviour
{
    [HideInInspector] public static UI Instance;
    [HideInInspector] public GlobalUI global;
    [HideInInspector] public LevelEditorUI editor;
    [HideInInspector] public PreloadUI preload; 
    [HideInInspector] public PauseUI pause;
    [HideInInspector] public WinUI win;
    [HideInInspector] public IngameUI ingame;
    [HideInInspector] public DialogUI dialog;
    [HideInInspector] public Selectors selectors;

    private void Awake()
    {
        // Singleton (UI has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(transform.parent.gameObject); return; }
        DontDestroyOnLoad(transform.parent.gameObject);

        // UI References!
        global = new() { self = gameObject };
        global.debugger = global.self.transform.Find("Debugger").GetComponent<Text>();
        global.debugger.CrossFadeAlpha(0, 0, true);
        global.camera = Camera.main;

        // Editor menu
        editor = new() { self = transform.Find("Level Editor Menu").gameObject };
        editor.importMenu = editor.self.transform.Find("Import").gameObject;
        editor.exportMenu = editor.self.transform.Find("Export").gameObject;
        editor.playtest = editor.self.transform.Find("Play Button").gameObject;

        // Win screen
        win = new() { self = transform.Find("Win Screen").gameObject };
        win.editorButton = win.self.transform.Find("Edit Level Button").gameObject;
        win.nextLevel = win.self.transform.Find("Next Level Button").gameObject;
        win.menuButton = win.self.transform.Find("Menu Button").gameObject;
        win.stats = win.self.transform.Find("Level Stats");
        win.time = win.stats.Find("Win Time").GetComponent<Text>();
        win.moves = win.stats.Find("Win Moves").GetComponent<Text>();

        // Preload / intermissions
        preload = new() { self = transform.parent.Find("Intermissions").Find("Level Load").gameObject };
        preload.animator = preload.self.transform.parent.GetComponent<Animator>();
        preload.levelName = preload.self.transform.Find("Level Name").gameObject.GetComponent<Text>();
        preload.stars = preload.self.transform.Find("Difficulty Stars").gameObject;
        preload.starFilledGraphic = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Filled");
        preload.starHollowGraphic = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Hollow");
        preload.time = preload.self.transform.Find("Best Time").gameObject.GetComponent<Text>();
        preload.moves = preload.self.transform.Find("Best Moves").gameObject.GetComponent<Text>();

        // Pause menu
        pause = new() { self = transform.Find("Pause Menu").gameObject };
        pause.editorButton = pause.self.transform.Find("Edit Level Button").gameObject;
        pause.backToMenu = pause.self.transform.Find("Menu Button").gameObject;
        pause.levelInfo = pause.self.transform.Find("Level Info");
        pause.levelBestMoves = pause.levelInfo.Find("Best Moves").GetComponent<Text>();
        pause.levelBestTime = pause.levelInfo.Find("Best Time").GetComponent<Text>();

        // Ingame UI
        ingame = new() { self = transform.Find("Ingame UI").gameObject };
        ingame.levelName = ingame.self.transform.Find("Level Name").Find("Text").GetComponent<Text>();
        ingame.levelMoves = ingame.self.transform.Find("Moves Info").Find("Level Moves").GetComponent<Text>();
        ingame.levelTimer = ingame.self.transform.Find("Time Info").Find("Level Time").GetComponent<Text>();
        ingame.areaCount = ingame.self.transform.Find("Area Info").Find("Area Count").GetComponent<Text>();
        
        // Dialog UI
        dialog = new() { self = transform.Find("Dialog UI").gameObject };
        dialog.name = dialog.self.transform.Find("Name").GetComponent<Text>();
        dialog.text = dialog.self.transform.Find("Text").GetComponent<Text>();

        // Change from preload scene?
        SceneManager.LoadScene("Main Menu");
    }

    // Change scenes
    public void ChangeScene(string sceneName, bool doTransition = true)
    {
        // Move the UI selectors to its default place
        if (selectors)
        {
            if (!selectors.left || !selectors.right) return;
            selectors.left.SetParent(selectors.gameObject.transform);
            selectors.right.SetParent(selectors.gameObject.transform);
            selectors.left.anchoredPosition = Vector2.zero;
            selectors.right.anchoredPosition = Vector2.zero;
        }

        // Change scene after transition
        if (doTransition) TransitionManager.Instance.TransitionIn(Reveal, ActionChangeScene, sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    // Exit application
    public void ExitApplication() { Application.Quit(); }

    // Goes to main menu (scary)
    public void GoMainMenu()
    {
        if (SceneManager.GetActiveScene().name == "Level Editor") LevelManager.Instance.SaveLevel("Editor Mode!", LevelManager.Instance.levelEditorName);
        TransitionManager.Instance.TransitionIn<string>(Reveal, ActionGoMainMenu);
    }

    // Go from a level to the editor
    public void GoLevelEditor()
    {
        if (!GameManager.Instance.IsDebug() && !GameManager.save.game.hasCompletedGame) { global.SendMessage("Complete the game first!"); return; }

        // Transition in
        TransitionManager.Instance.TransitionIn<string>(Reveal, ActionGoLevelEditor);
    }

    // Pause/Unpause game
    public void PauseUnpauseGame(bool status)
    {
        LevelManager.Instance.PauseResumeGame(status);
    }

    // Clears the UI (disables everything)
    private void ClearUI()
    {
        ingame.Toggle(false);
        editor.Toggle(false);
        pause.Toggle(false);
        win.Toggle(false);
    }

    // Import level
    public void LevelEditorImportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.LoadLevel(levelName, true, false); }

    // Export level
    public void LevelEditorExportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.SaveLevel(levelName, default, false); }

    // Playtest level
    public void LevelEditorPlaytest()
    {
        LevelManager.Instance.SaveLevel("Editor Mode!", LevelManager.Instance.levelEditorName);
        LevelManager.Instance.currentLevel = LevelManager.Instance.GetLevel(LevelManager.Instance.levelEditorName, true);
        LevelManager.Instance.currentLevelID = LevelManager.Instance.levelEditorName;
        GameManager.Instance.isEditing = true;
        LevelManager.Instance.worldOffsetX = 0;
        LevelManager.Instance.worldOffsetY = 0;
        LevelManager.Instance.MoveTilemaps(LevelManager.Instance.originalPosition, true);
        ingame.SetLevelName("Editor Mode!");
        editor.Toggle(false);
        ingame.Toggle(true);
        
        LevelManager.Instance.LoadLevel(LevelManager.Instance.levelEditorName, true);
        ChangeScene("Game");
    }

    // Goto next level
    public void GoNextLevel()
    {
        if (LevelManager.Instance.IsStringEmptyOrNull(LevelManager.Instance.currentLevel.nextLevel)) return;
        TransitionManager.Instance.TransitionIn<string>(Triangle, ActionGoNextLevel);
    }

    // Restart current level
    public void RestartLevel()
    {
        if (LevelManager.Instance.currentLevel == null) return;
        TransitionManager.Instance.TransitionIn<string>(Swipe, ActionRestartLevel);
    } 

    // Object classes
    public abstract class UIObject
    {
        public GameObject self;

        public void Toggle(bool status) { if (self) self.SetActive(status); }
    }

    public class GlobalUI : UIObject
    {
        public Camera camera;
        public Text debugger;

        // Sends a message log to the editor UI
        public void SendMessage(string message, float duration = 1.0f)
        {
            debugger.text = message;
            debugger.CrossFadeAlpha(1, 0, true);
            debugger.CrossFadeAlpha(0, duration, true);
        }
    }

    public class LevelEditorUI : UIObject
    {
        public GameObject importMenu;
        public GameObject exportMenu;
        public GameObject playtest;
    }

    public class PreloadUI : UIObject
    {
        public Animator animator;
        public Text levelName;
        public GameObject stars;
        public Sprite starFilledGraphic;
        public Sprite starHollowGraphic;
        public Text time;
        public Text moves;

        public void SetStars(int amount = 1)
        {
            if (amount < 1) amount = 1;
            else if (amount > 9) amount = 9;
            
            // Get list of stars
            List<Image> starList = new();
            foreach (Transform child in stars.transform.Find("Stars")) { starList.Add(child.GetComponent<Image>()); }

            // Enable stars
            starList.ForEach(s => s.sprite = starHollowGraphic);
            foreach (var star in starList)
            {
                if (amount <= 0) break;

                star.sprite = starFilledGraphic;
                amount--;
            }
        }
        public void SetLevelName(string name) { levelName.text = name; }

        public void SetBestTime(float newTime = -1)
        {
            if (newTime == -1 ) time.text = "Best time: ???";
            else time.text = $"Best time: {Math.Round(newTime, 2)}s";
        }

        public void SetBestMoves(int newMoves = -1)
        {
            if (newMoves == -1) moves.text = "Best moves: ???";
            else moves.text = $"Best moves: {newMoves}";
        }

        internal void PreparePreloadScreen(Serializables.GameData.Level save)
        {
            SetLevelName(LevelManager.Instance.currentLevel.levelName);
            SetStars(LevelManager.Instance.currentLevel.difficulty);
            if (save != null) { SetBestTime(save.stats.bestTime); SetBestMoves(save.stats.totalMoves); }
            else { SetBestTime(-1); SetBestMoves(-1); }
            Toggle(true);
            animator.Play("Load In");
        }
    }

    public class WinUI : UIObject
    {
        public GameObject editorButton;
        public GameObject nextLevel;
        public GameObject menuButton;
        public Transform stats;
        public Text time;
        public Text moves;

        public void ToggleEditButton(bool toggle) { editorButton.SetActive(toggle); }
        public void ToggleNextLevel(bool toggle) { nextLevel.SetActive(toggle); }
        public void SetTotalTime(float newTime) { time.text = $"Total time: {Math.Round(newTime, 2)}s"; }
        public void SetTotalMoves(int newMoves) { moves.text = $"Total moves: {newMoves}"; }
    }

    public class PauseUI : UIObject
    {
        public GameObject editorButton;
        public GameObject backToMenu;
        public Transform levelInfo;
        public Text levelBestTime;
        public Text levelBestMoves;

        public void ToggleEditButton(bool toggle) { editorButton.SetActive(toggle); }
        public void SetBestTime(float newTime) { levelBestTime.text = $"Best time: {Math.Round(newTime, 2)}s"; }
        public void SetBestMoves(int newMoves) { levelBestMoves.text = $"Best moves: {newMoves}"; }
    }

    public class IngameUI : UIObject
    {
        public Text levelName;
        public Text levelMoves;
        public Text levelTimer;
        public Text areaCount;
        public void SetLevelName(string newName) { levelName.text = $"{newName}"; }
        public void SetLevelMoves(int newMoves) { levelMoves.text = $"{newMoves}"; }
        public void SetLevelTimer(float newTime) { levelTimer.text = $"{Math.Round(newTime, 2)}s"; }
        public void SetAreaCount(int current, int max)
        {
            // Area overlapped SFX
            int.TryParse(areaCount.text.Split("/")[0], out int areaNum);
            if (AudioManager.Instance && current > areaNum) AudioManager.Instance.PlaySFX(AudioManager.areaOverlap, 0.35f);

            // Update text
            areaCount.text = $"{current}/{max}";
        }
    }

    public class DialogUI : UIObject
    {
        public Text name;
        public Text text;
        public void SetText(string newText, bool additive = false)
        {
            if (additive) text.text += $"{newText}";
            else text.text = $"{newText}";
        }
    }

    // Actions //
    internal void ActionChangeScene(string sceneName)
    {
        // Load the scene
        SceneManager.LoadScene(sceneName);
    }
    private void ActionGoLevelEditor(string _)
    {
        if (SceneManager.GetActiveScene().name == "Game") LevelManager.Instance.ReloadLevel();
        else if (!LevelManager.Instance.LoadLevel("EditorSession", true))
        {
            // Create EditorSession if the file does not exist.
            LevelManager.Instance.SaveLevel("Editor Mode!", LevelManager.Instance.levelEditorName);
            LevelManager.Instance.LoadLevel("EditorSession", true);
        }

        LevelManager.Instance.RefreshGameVars();
        ChangeScene("Level Editor", false);
        ClearUI();
    }
    private void ActionGoNextLevel(string _)
    {
        var save = GameManager.save.game.levels.Find(level => level.levelID == LevelManager.Instance.currentLevel.nextLevel);

        // Loads the level
        LevelManager.Instance.RefreshGameVars();
        LevelManager.Instance.RefreshGameUI();
        LevelManager.Instance.LoadLevel(LevelManager.Instance.currentLevel.nextLevel);

        // Preload screen
        TransitionManager.Instance.ChangeTransition(Triangle);
        if (!LevelManager.Instance.currentLevel.hideUI) preload.PreparePreloadScreen(save);
        else TransitionManager.Instance.TransitionOut<string>();
    }

    private void ActionGoMainMenu(string _)
    {
        LevelManager.Instance.ClearLevel();
        LevelManager.Instance.hasWon = false;
        GameManager.Instance.isEditing = false;
        LevelManager.Instance.currentLevel = null;
        ClearUI();

        ChangeScene("Main Menu", false);
    }
    private void ActionRestartLevel(string _)
    {
        LevelManager.Instance.RefreshGameVars();
        LevelManager.Instance.RefreshGameUI();
        LevelManager.Instance.ReloadLevel(true);
        TransitionManager.Instance.TransitionOut<string>(Swipe);
    }
}
