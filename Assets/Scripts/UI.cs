using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Serializables.GameData;

public class UI : MonoBehaviour
{
    [HideInInspector] public static UI Instance;
    [HideInInspector] public GlobalUI global;
    [HideInInspector] public LevelEditorUI editor;
    [HideInInspector] public PauseUI pause;
    [HideInInspector] public WinUI win;

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

        // Win screen
        win = new() { self = transform.Find("Win Screen").gameObject };
        win.stats = win.self.transform.Find("Level Stats");
        win.time = win.stats.Find("Win Time").GetComponent<Text>();
        win.moves = win.stats.Find("Win Moves").GetComponent<Text>();

        // Pause menu
        pause = new() { self = transform.Find("Pause Menu").gameObject };
        if (!Application.isEditor) pause.self.transform.Find("Edit Level Button").gameObject.SetActive(false);
        pause.levelInfo = pause.self.transform.Find("Level Info");
        pause.levelName = pause.levelInfo.Find("Level Name").GetComponent<Text>();
        pause.levelTimer = pause.levelInfo.Find("Level Timer").GetComponent<Text>();
        pause.levelMoves = pause.levelInfo.Find("Level Moves").GetComponent<Text>();
        pause.levelBestMoves = pause.levelInfo.Find("Best Moves").GetComponent<Text>();
        pause.levelBestTime = pause.levelInfo.Find("Best Time").GetComponent<Text>();

        // Change from preload scene?
        if (SceneManager.GetActiveScene().name == "Preload") ChangeScene("Main Menu");
    }

    // Change scenes
    public void ChangeScene(string sceneName) { SceneManager.LoadScene(sceneName); }

    // Clear UI Change scene
    public void CleanChangeScene(string sceneName)
    {
        ChangeScene(sceneName);
        ClearUI();
    }

    // Exit application
    public void ExitApplication() { Application.Quit(); }

    // Goes to main menu (scary)
    public void GoMainMenu()
    {
        LevelManager.Instance.ClearLevel();
        LevelManager.Instance.hasWon = false;
        ClearUI();

        ChangeScene("Main Menu");
    }

    // Pause/Unpause game
    public void PauseUnpauseGame(bool status) { LevelManager.Instance.PauseResumeGame(status); }

    // Clears the UI (disables everything)
    private void ClearUI()
    {
        editor.Toggle(false);
        pause.Toggle(false);
        win.Toggle(false);
    }

    // Import level
    public void LevelEditorImportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.LoadLevel(levelName, true); }

    // Export level
    public void LevelEditorExportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.SaveLevel(levelName); }

    // Playtest level
    public void LevelEditorPlaytest()
    {
        LevelManager.Instance.SaveLevel(LevelManager.Instance.levelEditorName, LevelManager.Instance.levelEditorName, true);
        LevelManager.Instance.currentLevel = LevelManager.Instance.GetLevel(LevelManager.Instance.levelEditorName, true);
        LevelManager.Instance.currentLevelID = LevelManager.Instance.levelEditorName;
        editor.Toggle(false);
        ChangeScene("Game");
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
    }

    public class WinUI : UIObject
    {
        public Transform stats;
        public Text time;
        public Text moves;

        public void SetTotalTime(float newTime) { time.text = $"Total time: {Math.Round(newTime, 2)}"; }
        public void SetTotalMoves(int newMoves) { moves.text = $"Total moves: {newMoves}"; }
    }

    public class PauseUI : UIObject
    {
        public Transform levelInfo;
        public Text levelName;
        public Text levelTimer;
        public Text levelMoves;
        public Text levelBestTime;
        public Text levelBestMoves;

        public void SetLevelName(string newName) { levelName.text = $"Level: {newName}"; }
        public void SetLevelTimer(float newTime) { levelTimer.text = $"Time: {Math.Round(newTime, 2)}s"; }
        public void SetLevelMoves(int newMoves) { levelMoves.text = $"Moves: {newMoves}"; }
        public void SetBestTime(float newTime) { levelBestTime.text = $"Best time: {Math.Round(newTime, 2)}s"; }
        public void SetBestMoves(int newMoves) { levelBestMoves.text = $"Best moves: {newMoves}"; }
    }
}
