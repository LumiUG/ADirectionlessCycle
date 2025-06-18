using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using Steamworks;
using Discord;
using Rect = UnityEngine.Rect;
using static Serializables;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public static GameManager I;
    [HideInInspector] public static Discord.Discord rpc = null;
    public Font originalFont;
    public Font acessibilityFont;

    // Game data // 
    [HideInInspector] public bool isEditing;
    [HideInInspector] public bool buildDebugMode;
    [HideInInspector] public bool chessbattleadvanced;
    [HideInInspector] public bool editormimic;
    [HideInInspector] public string currentEditorLevelID;
    [HideInInspector] public string currentEditorLevelName;
    public static Savedata save;
    public static string customLevelPath;

    internal List<TrialScriptable> trialsAreaOne;
    internal List<TrialScriptable> trialsAreaTwo;
    internal List<TrialScriptable> trialsAreaThree;
    internal List<TrialScriptable> trialsRemix;
    internal bool isDoingTrial = false;
    internal int lastSelectedWorld = 0;
    internal Color boxColor;
    internal Color remixColor;
    internal Color outboundColor;
    internal Color completedColor;
    internal readonly string[] noGameplayScenes = { "Main Menu", "Custom Levels", "Settings", "Credits", "Hub", "Bonus" };
    private readonly string[] badScenes = { "Main Menu", "Level Editor", "Custom Levels", "Settings", "Credits", "Hub", "Bonus" };
    private string dataPath;
    private ActivityTimestamps sessionTime = new();

    void Awake()
    {
        // Singleton (GameManager has persistence)
        if (!I) { I = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Game data
        dataPath = $"{Application.persistentDataPath}/userdata.save";

        // Create a savefile if none exist
        customLevelPath = $"{Application.persistentDataPath}/Custom Levels";
        CreateSave();

        // Create custom levels directory
        if (!Directory.Exists(customLevelPath)) Directory.CreateDirectory(customLevelPath);

        // Load the savefile
        LoadDataJSON();

        // Default variables
        trialsAreaOne = Resources.LoadAll<TrialScriptable>("Trials/Area 1").ToList();
        trialsAreaTwo = Resources.LoadAll<TrialScriptable>("Trials/Area 2").ToList();
        trialsAreaThree = Resources.LoadAll<TrialScriptable>("Trials/Area 3").ToList();
        trialsRemix = Resources.LoadAll<TrialScriptable>("Trials/Remix").ToList();
        currentEditorLevelID = null;
        currentEditorLevelName = null;
        chessbattleadvanced = false;
        editormimic = false;
        buildDebugMode = false;
        isEditing = false;

        // Colors!!
        ColorUtility.TryParseHtmlString("#62C7FF", out boxColor);
        ColorUtility.TryParseHtmlString("#E5615F", out remixColor);
        ColorUtility.TryParseHtmlString("#A22BE3", out outboundColor);
        ColorUtility.TryParseHtmlString("#4CF832", out completedColor);

        // Set master and SFX values
        if (AudioManager.I) {
            AudioManager.I.SetMasterVolume(save.preferences.masterVolume);
            // AudioManager.I.SetSFXVolume(save.preferences.SFXVolume); // not needed, we already use the variable!
        }

        // Init Discord RCP
        try {
            rpc = new Discord.Discord(1244697907723108362, (ulong)CreateFlags.NoRequireDiscord);
            sessionTime.Start = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        }
        catch { } // Dont care.
    }

    // Setup all the initial presences
    private void Start()
    {
        SetPresence("steam_display", "#Menuing");
        UpdateActivity("On the main menu.");
    }

    // Exclusively for Discord's bad SDK
    private void Update()
    {
        if (rpc == null) return;

        try { rpc.RunCallbacks(); }
        catch (ResultException) { rpc?.Dispose(); rpc = null; }
        catch { } // Dont care.
    }

    // Save game on leaving
    void OnDisable()
    {
        SaveDataJSON(save);
        rpc?.GetActivityManager().ClearActivity(null);
    }

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

    // Debug check
    public bool IsDebug()
    {
        return buildDebugMode || Application.isEditor;
    }

    // Pauses or resumes the game.
    public void PauseResumeGame(bool status)
    {
        if (LevelManager.I.voidedCutscene) return;

        if (status) {
            UI.I.selectors.ChangeSelected(UI.I.pause.resumeButton, true);
            UI.I.pause.ToggleEditButton(isEditing || IsDebug());
        }

        UI.I.pause.Toggle(status);
        LevelManager.I.isPaused = status;
    }

    public TrialScriptable IngameTrialSetup(string levelID)
    {
        if (levelID.StartsWith("W1")) return trialsAreaOne.Find(t => { return t.levelID == levelID; });
        if (levelID.StartsWith("W2")) return trialsAreaTwo.Find(t => { return t.levelID == levelID; });
        if (levelID.StartsWith("W3")) return trialsAreaThree.Find(t => { return t.levelID == levelID; });
        if (levelID.StartsWith("REMIX")) return trialsRemix.Find(t => { return t.levelID == levelID; });
        return null;
    }

    // Stuff with savedata //
    
    // Creates a savefile
    public void CreateSave(bool load = false)
    {
        if (!File.Exists(dataPath)) SaveDataJSON(new Savedata());
        if (load) LoadDataJSON();
    }

    // Deletes a savefile
    public void DeleteSave()
    {
        if (File.Exists(dataPath)) File.Delete(dataPath);
    }

    // Save user data
    public void SaveDataJSON(Savedata savedata = null)
    {
        savedata ??= save;
        File.WriteAllText(dataPath, JsonUtility.ToJson(savedata));
    }

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
        if (!level.completed) level.completed = changes.completed;
        if (!level.outboundCompletion) level.outboundCompletion = changes.outbound;
        if (changes.time != -1) level.stats.bestTime = (compareBest && (changes.time < level.stats.bestTime || level.stats.bestTime == 0f)) ? changes.time : level.stats.bestTime;

        // (moves)
        int calc = (compareBest && (changes.moves < level.stats.totalMoves || level.stats.totalMoves == 0)) ? changes.moves : level.stats.totalMoves;
        if (changes.moves != -1) level.stats.totalMoves = calc;

        if (!changes.completed) return;
        if (LevelManager.I.hasCycledInCurrentAttempt && changes.moves != -1) level.stats.totalMovesCycle = calc;
        if (!LevelManager.I.hasCycledInCurrentAttempt && changes.moves != -1) level.stats.totalMovesNormal = calc;
    }

    // Saves a level preview when exporting from the editor
    internal byte[] SaveLevelPreview()
    {
        if (SceneManager.GetActiveScene().name != "Level Editor") return null;
        RenderTexture texture = Resources.Load<RenderTexture>("Misc/Screenshot");

        // Convert to Texture2D
        Texture2D tex = new(1920, 1080, TextureFormat.RGB24, false, true);
        RenderTexture.active = texture;
        tex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        tex.Apply();

        // Encode and return
        return tex.EncodeToPNG(); 
    }
    
    // Converts a base 64 string to a texture, usually used with level preview textures
    public Texture2D Base64ToTexture(string image)
    {
        Texture2D texture = new(1920, 1080);
        byte[] bytes = Convert.FromBase64String(image);
        ImageConversion.LoadImage(texture, bytes);
        return texture;
    }

    // Steam Integration //

    // Grants an achievement, then stores it to server
    // Example: EditAchivement("ACH_TEST");
    internal void EditAchivement(string id, bool grant = true)
    {
        if (!SteamManager.Initialized || id == null) return;

        if (grant) SteamUserStats.SetAchievement(id);
        else SteamUserStats.ClearAchievement(id);

        SteamUserStats.StoreStats();
    }

    // Grants an achievement, then stores it to server
    // Example key: SetPresence("steam_display", "#Menuing");
    // Example variable: SetPresence("currentlevel", "Level Name!");
    // https://steamcommunity.com/dev/testrichpresence
    internal void SetPresence(string key, string display)
    {
        if (!SteamManager.Initialized) return;

        SteamFriends.SetRichPresence(key, display);
    }

    // Discord RPC //
    public void UpdateActivity(string details)
    {
        if (rpc == null || string.IsNullOrEmpty(details)) return;
		var activityManager = rpc.GetActivityManager();

        // Setup activity
        var assets = new ActivityAssets();
        if (LevelManager.I.currentLevel != null && details.Contains("Playing"))
        {
            if (LevelManager.I.currentLevelID.Contains("W1")) { assets.LargeImage = "one"; assets.LargeText = "Currently in Area One"; }
            else if (LevelManager.I.currentLevelID.Contains("W2")) { assets.LargeImage = "two"; assets.LargeText = "Currently in Area Two"; }
            else if (LevelManager.I.currentLevelID.Contains("W3")) { assets.LargeImage = "three"; assets.LargeText = "Currently in Area Three"; }
            else if (LevelManager.I.currentLevelID.Contains("REMIX")) { assets.LargeImage = "remix"; assets.LargeText = "Currently inverted."; }
            else if (LevelManager.I.currentLevelID.Contains("VOID") || LevelManager.I.currentLevelID.Contains("ORB") || LevelManager.I.currentLevelID.Contains("FRAGMENT")) { assets.LargeImage = "core"; assets.LargeText = "Currently gazing nothingness."; }
            else if (LevelManager.I.currentLevelID.Contains("HINT")) { assets.LargeImage = "custom"; assets.LargeText = "Currently viewing a hint!"; }
            else { assets.LargeImage = "custom"; assets.LargeText = "Currently on a special/custom level."; }
            assets.SmallImage = "star";
            assets.SmallText = $"Time: {Math.Round(LevelManager.I.levelTimer, 2)}s.\nMoves: {LevelManager.I.levelMoves}";
        } else activityManager.ClearActivity(null);

        // Update activity
		var activity = new Activity
		{
            Type = ActivityType.Playing,
			Details = details,
            Timestamps = sessionTime,
            Assets = assets
		};
		activityManager.UpdateActivity(activity, null);
    }
}
