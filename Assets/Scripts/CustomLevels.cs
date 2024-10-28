using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Serializables;
using static TransitionManager.Transitions;

public class CustomLevels : MonoBehaviour
{
    [HideInInspector] public static CustomLevels I;
    [HideInInspector] public int rowCount;
    public RectTransform holder;
    public GameObject backButton;
    public GameObject popup;
    public Text popupTitle;
    public GameObject popupExit;
    public Button popupPlay;
    public Button popupEdit;
    public Button popupDelete;
    public InputField popupLevelID;
    public InputField popupLevelName;
    public List<Image> popupStars = new();
    public readonly int vertical = -700;

    private string selectedLevelID = null;
    private string selectedLevelName = null;
    private SerializableLevel selectedLevelAsData = null;
    private GameObject selectedLevel = null;
    private GameObject customLevelPrefab;
    private Sprite starSprite;
    private Sprite hollowStarSprite;
    private bool confirmDeletion = false;
    private int count = 0;
    private bool shouldReloadLevels = false;

    void Start()
    {
        I = this; // No persistence!
        EventSystem.current.SetSelectedGameObject(backButton);
        customLevelPrefab = Resources.Load<GameObject>("Prefabs/Custom Level");
        starSprite = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Filled");
        hollowStarSprite = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Hollow");

        // Load all custom levels
        LoadCustomLevels();
    }

    // Scrolling
    void Update() 
    {
        if (EventSystem.current == null) return;
        if (selectedLevel == EventSystem.current.currentSelectedGameObject || EventSystem.current.currentSelectedGameObject.transform.parent.name != "Content") return;
        selectedLevel = EventSystem.current.currentSelectedGameObject;

        holder.anchoredPosition = new Vector2(holder.anchoredPosition.x, selectedLevel.GetComponent<RectTransform>().anchoredPosition.y * -1 - 540);
    }

    // Refreshes custom levels
    public void RefreshCustomLevels(string filter = null)
    {
        // Clear all levels and re-load everything
        foreach (Transform level in holder.transform) { Destroy(level.gameObject); }
        holder.anchoredPosition = new Vector2(holder.anchoredPosition.x, -540);
        LoadCustomLevels(filter);
    }

    // Loads all custom levels
    internal void LoadCustomLevels(string filter = null) 
    {
        rowCount = -1;
        count = 0;

        foreach (string fileName in Directory.GetFiles(GameManager.customLevelPath))
        {
            if (!fileName.EndsWith(".level") || fileName.Contains($"{LevelManager.Instance.levelEditorName}.level")) continue;
            if (count == 0) rowCount++;
            Texture2D preview = null;
            count++;

            // Get level info & preview image
            string levelID = fileName.Replace(".level", "").Replace(GameManager.customLevelPath, "").Replace("\\", "");
            if (filter != null && !levelID.ToLower().Contains(filter.ToLower())) { if (count == 1) rowCount--; count--; continue; }
            Serializables.SerializableLevel level = LevelManager.Instance.GetLevel(levelID, true);
            if (!LevelManager.Instance.IsStringEmptyOrNull(level.previewImage)) preview = GameManager.Instance.Base64ToTexture(level.previewImage);

            // Create prefab and set position
            GameObject entry = Instantiate(customLevelPrefab, holder);
            entry.name = level.levelName;
            if (count == 1) entry.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, vertical * rowCount);
            else {
                entry.GetComponent<RectTransform>().anchoredPosition = new Vector2(550, vertical * rowCount);
                count = 0;
            }

            // Prefab basic data
            entry.transform.Find("Name").GetComponent<Text>().text = $"\"{level.levelName}\"";
            if (preview != null) entry.transform.Find("Preview").GetComponent<RawImage>().texture = preview;

            // Prefab stars
            Transform stars = entry.transform.Find("Stars");
            for (int i = 0; i < level.difficulty; i++) { stars.Find($"{i + 1}").GetComponent<Image>().sprite = starSprite; }

            // Prefab load level
            entry.GetComponent<Button>().onClick.AddListener(delegate { OpenLevelMenu(levelID, level.levelName); });
        }
    }

    // Player Interactions //

    // Open a level's menu
    private void OpenLevelMenu(string levelID, string levelName)
    {
        EventSystem.current.SetSelectedGameObject(popupPlay.gameObject);
        selectedLevelName = levelName;
        selectedLevelID = levelID;
        selectedLevelAsData = LevelManager.Instance.GetLevel(selectedLevelID, true);
        SetStarSprites(selectedLevelAsData.difficulty);
        popupPlay.interactable = true;
        popupEdit.interactable = true;
        popupDelete.interactable = true;
        popupLevelID.interactable = true;
        popupLevelName.interactable = true;
        popup.SetActive(true);
        popupLevelID.text = selectedLevelID;
        popupLevelName.text = selectedLevelAsData.levelName;
    }

    // Close a level's menu
    public void CreateLevel()
    {
        if (!GameManager.Instance.IsDebug() && !GameManager.save.game.hasCompletedGame)
        {
            UI.Instance.global.SendMessage("Complete the game first!");
            CloseLevelMenu();
            return;
        }

        selectedLevelID = LevelManager.Instance.SaveLevel("New level", default, true, null);
        selectedLevelName = "New level";
        EditLevel();
    }

    // Close a level's menu
    public void CloseLevelMenu()
    {
        if (shouldReloadLevels) RefreshCustomLevels();

        popupTitle.text = "Level Menu";
        selectedLevelAsData = null;
        confirmDeletion = false;
        shouldReloadLevels = false;
        EventSystem.current.SetSelectedGameObject(backButton);
        popup.SetActive(false);
    }

    // Load current level
    public void PlayLevel()
    {
        TransitionManager.Instance.TransitionIn(Reveal, LevelManager.Instance.ActionLoadLevel, selectedLevelID);
    }

    // Edit current level
    public void EditLevel()
    {
        if (!GameManager.Instance.IsDebug() && !GameManager.save.game.hasCompletedGame)
        {
            UI.Instance.global.SendMessage("Complete the game first!");
            CloseLevelMenu();
            return;
        }

        GameManager.Instance.currentEditorLevelID = selectedLevelID;
        GameManager.Instance.currentEditorLevelName = selectedLevelName;

        string content = File.ReadAllText($"{GameManager.customLevelPath}/{selectedLevelID}.level");
        File.WriteAllText($"{GameManager.customLevelPath}/{LevelManager.Instance.levelEditorName}.level", content);

        UI.Instance.GoLevelEditor();
    }

    // Delete current level
    public void DeleteLevel()
    {
        // Confirm to the user if they really want to delete the level
        if (!confirmDeletion)
        {
            AudioManager.Instance.PlaySFX(AudioManager.tileDeath, 0.30f);
            popupTitle.text = "Are you sure?";
            confirmDeletion = true;
            return;
        }

        // Delete the level!
        string deletionPath = $"{GameManager.customLevelPath}/{selectedLevelID}.level";
        if (File.Exists(deletionPath)) File.Delete(deletionPath);
        RefreshCustomLevels();

        // Deletion visuals
        EventSystem.current.SetSelectedGameObject(popupExit);
        AudioManager.Instance.PlaySFX(AudioManager.areaOverlap, 0.30f);
        popupTitle.text = "Level deleted!";
        popupPlay.interactable = false;
        popupEdit.interactable = false;
        popupDelete.interactable = false;
        popupLevelID.interactable = false;
        popupLevelName.interactable = false;
        confirmDeletion = false;
    }

    // Changes the current level name
    public void ChangeLevelName(string value)
    {
        if (LevelManager.Instance.IsStringEmptyOrNull(value)) return;

        selectedLevelAsData.levelName = value;
        File.WriteAllText($"{GameManager.customLevelPath}/{selectedLevelID}.level", JsonUtility.ToJson(selectedLevelAsData, false));
        
        UI.Instance.global.SendMessage($"Level name set to \"{value}\"", 5f);
        RefreshCustomLevels();
        CloseLevelMenu();
    }

    // Sets the current level difficulty
    public void SetLevelDifficulty(int difficulty)
    {
        if (selectedLevelAsData.difficulty == difficulty) return;
        
        selectedLevelAsData.difficulty = difficulty;
        File.WriteAllText($"{GameManager.customLevelPath}/{selectedLevelID}.level", JsonUtility.ToJson(selectedLevelAsData, false));
        SetStarSprites(difficulty);
        shouldReloadLevels = true;
    }

    // Changes a Level's ID
    public void ChangeLevelID(string newID)
    {
        if (LevelManager.Instance.IsStringEmptyOrNull(newID)) return;

        // Rename file if level ID changed
        string cleanID = string.Concat(newID.Split(Path.GetInvalidFileNameChars()));
        if (File.Exists($"{GameManager.customLevelPath}/{cleanID}.level")) return;

        // Rename file and refresh levels
        File.Move(
            $"{GameManager.customLevelPath}/{selectedLevelID}.level",
            $"{GameManager.customLevelPath}/{cleanID}.level");
        selectedLevelID = cleanID;

        UI.Instance.global.SendMessage($"Level ID set to \"{newID}\"", 5f);
        RefreshCustomLevels();
        CloseLevelMenu();
    }

    // Open the custom level folder
    public void OpenCustomLevelFolder()
    {
        Application.OpenURL(GameManager.customLevelPath);
    }

    // Enables/disables stars
    private void SetStarSprites(int index)
    {
        popupStars.ForEach(star => star.sprite = hollowStarSprite);
        for (int i = 0; i < index; i++) { popupStars[i].sprite = starSprite; }
    }
}
