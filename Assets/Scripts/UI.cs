using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    [HideInInspector] public static UI Instance;
    [HideInInspector] public LevelEditorUI editor;

    private void Awake()
    {
        // Singleton (UI has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(transform.parent.gameObject); return; }
        DontDestroyOnLoad(transform.parent.gameObject);

        // UI References!
        editor = new();
        editor.self = transform.Find("Level Editor Menu").gameObject;
        editor.importMenu = editor.self.transform.Find("Import").gameObject;
        editor.exportMenu = editor.self.transform.Find("Export").gameObject;
    }

    // Change scenes
    public void ChangeScene(string sceneName) { SceneManager.LoadScene(sceneName); }

    // Exit application
    public void ExitApplication() { Application.Quit(); }

    // Master Slider
    public void UpdateMasterSlider(float value) { Debug.Log($"Master: {value}"); }

    // SFX Slider
    public void UpdateSFXSlider(float value) { Debug.Log($"SFX: {value}"); }

    // Import level (move to LevelEditorUI?)
    public void LevelEditorImportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.LoadLevel(levelName); }

    // Export level (move to LevelEditorUI?)
    public void LevelEditorExportLevel(string levelName) { if (LevelManager.Instance) LevelManager.Instance.SaveLevel(levelName); }


    // Object classes
    public abstract class UIObject
    {
        public GameObject self;
    }

    public class LevelEditorUI : UIObject
    {
        public GameObject importMenu;
        public GameObject exportMenu;

        public void Toggle(bool status) { self.SetActive(status); }
    }
}
