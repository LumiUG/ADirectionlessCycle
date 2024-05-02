using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        // Editor menu
        editor = new() { self = transform.Find("Level Editor Menu").gameObject };
        editor.importMenu = editor.self.transform.Find("Import").gameObject;
        editor.exportMenu = editor.self.transform.Find("Export").gameObject;

        // Win screen
        win = new() { self = transform.Find("Win Screen").gameObject };

        // Pause menu
        pause = new() { self = transform.Find("Pause Menu").gameObject };

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

    // Master Slider
    public void UpdateMasterSlider(float value) { Debug.Log($"Master: {value}"); }

    // SFX Slider
    public void UpdateSFXSlider(float value) { Debug.Log($"SFX: {value}"); }

    // Goes to main menu (scary)
    public void GoMainMenu()
    {
        LevelManager.Instance.ClearLevel();
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


    // Import level (move to LevelEditorUI?)
    public void LevelEditorImportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.LoadLevel(levelName); }

    // Export level (move to LevelEditorUI?)
    public void LevelEditorExportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.SaveLevel(levelName); }

    // Playtest level (move to LevelEditorUI?)
    public void LevelEditorPlaytest() { editor.Toggle(false); ChangeScene("Game"); }

    // Object classes
    public abstract class UIObject
    {
        public GameObject self;

        public void Toggle(bool status) { if (self) self.SetActive(status); }
    }

    public class GlobalUI : UIObject
    {
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

    public class WinUI : UIObject { }

    public class PauseUI : UIObject { }
}
