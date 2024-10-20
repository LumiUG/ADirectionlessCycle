using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomLevels : MonoBehaviour
{
    public RectTransform holder;
    private GameObject customLevelPrefab;
    private Sprite starSprite;
    private int rowCount = 0;

    void Start()
    {
        customLevelPrefab = Resources.Load<GameObject>("Prefabs/Custom Level");
        starSprite = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Filled");
        int vertical = -700;
        int count = 0;

        // Load all custom levels
        foreach (string fileName in Directory.GetFiles(GameManager.customLevelPath))
        {
            if (!fileName.EndsWith(".level")) continue;
            Texture2D preview = null;
            count++;

            // Get level info & preview image
            Serializables.SerializableLevel level = LevelManager.Instance.GetLevel(fileName.Replace(".level", "").Replace(GameManager.customLevelPath, ""), true);
            if (!LevelManager.Instance.IsStringEmptyOrNull(level.previewImage)) preview = GameManager.Instance.Base64ToTexture(level.previewImage);

            // Create prefab and set position
            GameObject entry = Instantiate(customLevelPrefab, holder);
            if (count == 1) entry.GetComponent<RectTransform>().anchoredPosition = new Vector2(-650, vertical * rowCount);
            else if (count == 2) entry.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, vertical * rowCount);
            else {
                entry.GetComponent<RectTransform>().anchoredPosition = new Vector2(650, vertical * rowCount);
                rowCount++;
                count = 0;
            }

            // Fill prefab data
            entry.transform.Find("Name").GetComponent<Text>().text = $"\"{level.levelName}\"";
            if (preview != null) entry.transform.Find("Preview").GetComponent<RawImage>().texture = preview;

            Transform stars = entry.transform.Find("Stars");
            for (int i = 0; i < level.difficulty; i++)
            {
                stars.Find($"{i + 1}").GetComponent<Image>().sprite = starSprite;
            }
        }

        EventSystem.current.SetSelectedGameObject(transform.Find("Back Button").gameObject);
    }

    // void Update()
    // {
    //     rt.anchoredPosition += Time.deltaTime * (Vector2.up * 22f);  
    // }
}
