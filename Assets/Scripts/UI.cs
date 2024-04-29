using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    [HideInInspector] public static UI Instance;
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
        editor = new() { self = transform.Find("Level Editor Menu").gameObject };
        editor.importMenu = editor.self.transform.Find("Import").gameObject;
        editor.exportMenu = editor.self.transform.Find("Export").gameObject;

        win = new() { self = transform.Find("Win Screen").gameObject };

        // Preload?
        if (SceneManager.GetActiveScene().name == "Preload") ChangeScene("Main Menu");
    }

    // Change scenes
    public void ChangeScene(string sceneName) { SceneManager.LoadScene(sceneName); }

    // Exit application
    public void ExitApplication() { Application.Quit(); }

    // Master Slider
    public void UpdateMasterSlider(float value) { Debug.Log($"Master: {value}"); }

    // SFX Slider
    public void UpdateSFXSlider(float value) { Debug.Log($"SFX: {value}"); }

    // Goes to main menu (scary)
    public void GoMainMenu()
    {
        editor.Toggle(false);
        //pause.Toggle(false);
        win.Toggle(false);

        LevelManager.Instance.ClearLevel();

        ChangeScene("Main Menu");
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

    public class LevelEditorUI : UIObject
    {
        public GameObject importMenu;
        public GameObject exportMenu;
    }
    public class WinUI : UIObject
    {

    }

    public class PauseUI : UIObject
    {

    }
}
