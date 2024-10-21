using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TransitionManager.Transitions;

public class CustomLevels : MonoBehaviour
{
    [HideInInspector] public static CustomLevels I;
    public RectTransform holder;
    public readonly int vertical = -700;
    public int rowCount = 0;

    private GameObject selectedLevel = null;
    private GameObject customLevelPrefab;
    private Sprite starSprite;
    private int count = 0;

    void Start()
    {
        I = this; // No persistence!
        EventSystem.current.SetSelectedGameObject(transform.Find("Back Button").gameObject);
        customLevelPrefab = Resources.Load<GameObject>("Prefabs/Custom Level");
        starSprite = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Filled");

        // Load all custom levels
        LoadCustomLevels();
    }

    void Update() 
    {
        if (EventSystem.current == null) return;
        if (selectedLevel == EventSystem.current.currentSelectedGameObject || EventSystem.current.currentSelectedGameObject.transform.parent.name != "Content") return;
        selectedLevel = EventSystem.current.currentSelectedGameObject;

        holder.anchoredPosition = new Vector2(holder.anchoredPosition.x, selectedLevel.GetComponent<RectTransform>().anchoredPosition.y * -1 - 540);
    }

    // Open folder
    public void OpenCustomLevelFolder()
    {
        Application.OpenURL(GameManager.customLevelPath);
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
            string levelID = fileName.Replace(".level", "").Replace(GameManager.customLevelPath, "");
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
            entry.GetComponent<Button>().onClick.AddListener(delegate { TransitionManager.Instance.TransitionIn(Reveal, LevelManager.Instance.ActionLoadLevel, levelID); });
        }
    }
}
