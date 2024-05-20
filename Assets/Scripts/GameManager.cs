using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using System.IO;
using static Serializables;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public static GameManager Instance;
    [HideInInspector] public AudioSource musicBox;
    
    // Game data // 
    public static Savedata save;
    public bool isEditing;
    public SpriteRenderer drawOver;
    private string dataPath;

    private readonly string[] badScenes = { "Main Menu", "Level Editor", "Settings", "Hub" };

    void Awake()
    {
        // Singleton (GameManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Game data
        dataPath = $"{Application.persistentDataPath}/userdata.save";

        // Create a savefile if none exist
        string levelDir = $"{Application.persistentDataPath}/Custom Levels";
        if (!File.Exists(dataPath)) SaveDataJSON(new Savedata());

        // Create custom levels directory
        if (!Directory.Exists(levelDir)) Directory.CreateDirectory(levelDir);

        // Load the savefile
        LoadDataJSON();

        // Default variables
        isEditing = false;

        // Fixes a stupid bug
        drawOver = Resources.Load("Prefabs/PingDrawOver").GetComponent<SpriteRenderer>();
        drawOver.sprite = null;
    }

    // Save game on leaving
    void OnDisable() { SaveDataJSON(save); }

    // Returns if the current scene shouldn't be taken into account
    public bool IsBadScene()
    {
        return badScenes.Contains(SceneManager.GetActiveScene().name);
    }

    // Editor check
    public bool IsEditor()
    {
        return SceneManager.GetActiveScene().name == "Level Editor";
    }

    // Stuff with savedata //
    
    // Save user data
    public void SaveDataJSON(Savedata save) { File.WriteAllText(dataPath, JsonUtility.ToJson(save)); }

    // Load user data
    public void LoadDataJSON() { save = JsonUtility.FromJson<Savedata>(File.ReadAllText(dataPath)); }

    // Mark level
    public void UpdateSavedLevel(string levelID, GameData.LevelChanges changes, bool compareBest = false)
    {
        // Get the level
        GameData.Level level = save.game.levels.Find(l => l.levelID == levelID);
        if (level == null)
        {
            level = new(levelID);
            save.game.levels.Add(level);
        }

        // Update the level
        level.completed = changes.completed;
        if (changes.time != -1) level.stats.bestTime = (compareBest && (changes.time < level.stats.bestTime || level.stats.bestTime == 0f)) ? changes.time : level.stats.bestTime;
        if (changes.moves != -1) level.stats.totalMoves = (compareBest && (changes.moves < level.stats.totalMoves || level.stats.totalMoves == 0)) ? changes.moves : level.stats.totalMoves;
    }
}
