using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TransitionManager.Transitions;
using static Serializables;
using static GameTile;
using System;

public class Hub : MonoBehaviour
{
    [HideInInspector] public static Hub I;
    public List<GameObject> worldHolders = new(capacity: 3);
    public List<RectTransform> hubArrows = new(capacity: 2);
    public GameObject worldHolder;
    public RectTransform outlineHolder;
    public GameObject backButton;
    public Checker checker;
    public Text levelName;


    private readonly int[] positions = { 0, -1920, -3840, -5760 };
    private readonly List<int> completedLevelsCount = new() { 2, 2, 2 };
    private Color remixColor;
    private Color outboundColor;
    private GameObject lastSelectedlevel = null;
    private RectTransform holderRT = null;
    private RectTransform previewRT = null;
    private int worldIndex = 0;

    private void Awake()
    {
        I = this; // No persistence!
    }

    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(backButton);
        holderRT = worldHolder.GetComponent<RectTransform>();
        previewRT = levelName.GetComponent<RectTransform>();

        // Colors!!
        ColorUtility.TryParseHtmlString("#E5615F", out remixColor);
        ColorUtility.TryParseHtmlString("#A22BE3", out outboundColor);

        // Set colors for locked levels
        for (int i = 0; i < worldHolders.Count; i++)
        {
            // Check all levels present in the hub for completion
            for (int j = 0; j < worldHolders[i].transform.childCount; j++)
            {
                Transform child = worldHolders[i].transform.GetChild(j);
                var levelCheck = GameManager.save.game.levels.Find(level => level.levelID == $"{worldHolders[i].name}/{child.name}");
                if (levelCheck == null) continue;

                // Add an outline and add a completed count
                if (levelCheck.completed)
                {
                    Transform outline = outlineHolder.Find(worldHolders[i].name).Find(child.name);
                    outline.gameObject.SetActive(true);
                    if (completedLevelsCount[i] < 12) completedLevelsCount[i]++;

                    var levelAsData = LevelManager.Instance.GetLevel(levelCheck.levelID, false, true);
                    int displayCheck = RecursiveHubCheck(levelAsData, levelCheck.levelID, false);
                    if (GameManager.save.game.mechanics.hasSeenRemix && displayCheck == 1) outline.GetComponent<Image>().color = remixColor;
                    else if (GameManager.save.game.mechanics.hasSeenOutbound && displayCheck == 2) outline.GetComponent<Image>().color = outboundColor;
                }
            }

            // Progress locking
            if (completedLevelsCount[0] < 12) {
                if (!GameManager.Instance.IsDebug()) { hubArrows[0].gameObject.SetActive(false); hubArrows[1].gameObject.SetActive(false); }
                completedLevelsCount[1] = 0;
                completedLevelsCount[2] = 0;
            }
            else if (completedLevelsCount[1] < 12) { completedLevelsCount[2] = 0; }

            // Sorry! We are looping again for available levels using the completed count!
            for (int j = 0; j < completedLevelsCount[i]; j++)
            { 
                Transform child = worldHolders[i].transform.GetChild(j);
                if (child)
                {
                    if (LevelManager.Instance.GetLevel($"{worldHolders[i].name}/{child.name}", false, true) == null) continue;
                    child.GetComponent<Image>().color = Color.white;
                }
            }
        }
    }

    // Cycle through levels
    private void Update()
    {
        if (!EventSystem.current) return;
        if (EventSystem.current.currentSelectedGameObject == null) return;

        // Checking if you swapped levels (condition)
        if (lastSelectedlevel == EventSystem.current.currentSelectedGameObject
        || !EventSystem.current.currentSelectedGameObject.transform.parent.name.StartsWith("W")) return;
        lastSelectedlevel = EventSystem.current.currentSelectedGameObject;

        // Update UI
        string levelID = $"{lastSelectedlevel.transform.parent.name}/{lastSelectedlevel.name}";
        if (levelID.Contains(".")) levelID = $"REMIX/{lastSelectedlevel.name.Split("-")[1]}";
        
        SerializableLevel level = LevelManager.Instance.GetLevel(levelID, false, true);
        PreviewText(level, levelID);

        // Show proper remix levels attached
        if (!levelID.Contains("REMIX"))
        {
            if (!LevelManager.Instance.IsStringEmptyOrNull(level.remixLevel))
            {
                HideRevealUI(true);
                
            }
            else HideRevealUI(false);
        }
    }

    // Hides/unhides some UI elements while a valid level is selected (not hovered)
    private void HideRevealUI(bool toggle)
    {
        // Toggle on
        if (toggle)
        {
            backButton.GetComponent<RectTransform>().anchoredPosition = new(0, 75);
            previewRT.anchoredPosition = new(0, -75);
            hubArrows[0].gameObject.SetActive(false);
            hubArrows[1].gameObject.SetActive(false);
            return;
        }

        // Toggle off
        backButton.GetComponent<RectTransform>().anchoredPosition = new(0, 150);
        previewRT.anchoredPosition = new(0, -150);
        hubArrows[0].gameObject.SetActive(true);
        hubArrows[1].gameObject.SetActive(true);
    }

    // Now as a function for mouse hovers!
    public void PreviewText(SerializableLevel level, string levelID)
    {
        // Set the preview text
        if (level != null)
        {
            // Locked level?
            if (AbsurdLockedLevelDetection(levelID)) SetLevelName("???");
            else SetLevelName(level.levelName);
        }
        else SetLevelName("UNDER DEVELOPMENT");
    }

    // Set level name on the hub
    public void SetLevelName(string newText) { levelName.text = $"[ {newText} ]"; }

    // Load level
    public void StaticLoadLevel(string levelName)
    {
        if (!LevelManager.Instance) return;

        // Is the level locked?
        if (AbsurdLockedLevelDetection(levelName)) { AudioManager.Instance.PlaySFX(AudioManager.tileDeath, 0.25f); return; }

        // Plays the transition
        TransitionManager.Instance.TransitionIn(Reveal, LevelManager.Instance.ActionLoadLevel, levelName);
    }

    // Change world
    public void ChangeWorld(int direction)
    {
        if (worldIndex + direction >= positions.Length || worldIndex + direction < 0) return;

        if (!GameManager.Instance.IsDebug()) {
            if ((worldIndex + direction == 1 && completedLevelsCount[1] < 1) || (worldIndex + direction == 2 && completedLevelsCount[2] < 1)) return;
            if (worldIndex + direction == 3 && GameManager.save.game.collectedOrbs.Count < 1) return;
        }

        worldIndex += direction;
        holderRT.anchoredPosition = new(positions[worldIndex], holderRT.anchoredPosition.y);
        outlineHolder.anchoredPosition = new(positions[worldIndex], holderRT.anchoredPosition.y);

        // Update checker direction
        checker.dirX = direction;
        
        if (EventSystem.current.currentSelectedGameObject == hubArrows[0] || EventSystem.current.currentSelectedGameObject == hubArrows[1]) return;
        EventSystem.current.SetSelectedGameObject(backButton);
    }

    // Returns true if a level is locked.
    public bool AbsurdLockedLevelDetection(string fullLevelID)
    {
        if (LevelManager.Instance.GetLevel(fullLevelID, false, true) == null) return true;

        if (GameManager.Instance.IsDebug()) return false;

        string[] levelSplit = fullLevelID.Split("/")[1].Split("-");
        return completedLevelsCount[int.Parse(levelSplit[0]) - 1] < int.Parse(levelSplit[1]);
    }

    // bullshit basically
    private int RecursiveHubCheck(SerializableLevel level, string levelID, bool isRemix)
    {
        if (level == null) return 0;

        // Outerbound completion?
        if (level.tiles.overlapTiles.Exists(t => { return t.type == ObjectTypes.OutboundArea.ToString(); }) && GameManager.save.game.levels.Exists(l => { return l.levelID == levelID && l.outboundCompletion == false; })) return 2;
        
        // Remix completion? (DOESNT have priority RN)
        GameData.Level statCheck = GameManager.save.game.levels.Find(l => l.levelID == levelID);
        if (!isRemix) {
            if (statCheck == null) return 0;
            if (!statCheck.completed) return 0;
            if (level.remixLevel == null) return 0;
        } else {
            if (statCheck == null) return 1;
            if (!statCheck.completed) return 1;
            if (level.remixLevel == null) return 0;
        }

        return RecursiveHubCheck(LevelManager.Instance.GetLevel(level.remixLevel, false, true), level.remixLevel, true);
    }

    // Actions //
}
