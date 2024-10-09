using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TransitionManager.Transitions;
using static Serializables;
using static GameTile;

public class Hub : MonoBehaviour
{
    public List<GameObject> worldHolders = new(capacity: 3);
    public GameObject worldHolder;
    public RectTransform outlineHolder;
    public GameObject backButton;
    public Checker checker;
    public Text levelName;

    private readonly int[] positions = { 0 };
    private readonly List<int> completedLevelsCount = new() { 2 };
    private Color remixColor;
    private Color outboundColor;
    private GameObject lastSelectedlevel = null;
    private RectTransform holderRT = null;
    private int worldIndex = 0;

    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(backButton);
        holderRT = worldHolder.GetComponent<RectTransform>();

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

        // Checking if you swapped levels
        if (lastSelectedlevel == EventSystem.current.currentSelectedGameObject || !EventSystem.current.currentSelectedGameObject.transform.parent.name.StartsWith("W")) return;
        lastSelectedlevel = EventSystem.current.currentSelectedGameObject;
        PreviewText($"{lastSelectedlevel.transform.parent.name}/{lastSelectedlevel.name}");
    }

    // Now as a function for mouse hovers!
    public void PreviewText(string levelID)
    {
        // Set the preview text
        SerializableLevel level = LevelManager.Instance.GetLevel(levelID, false, true);
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
        TransitionManager.Instance.TransitionIn(Reveal, ActionLoadLevel, levelName);
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
        EventSystem.current.SetSelectedGameObject(backButton);
        checker.dirX = direction;
    }

    // Returns true if a level is locked.
    public bool AbsurdLockedLevelDetection(string fullLevelID)
    {
        if (LevelManager.Instance.GetLevel(fullLevelID, false, true) == null) return true;

        // if (GameManager.Instance.IsDebug()) return false;

        if (fullLevelID.Contains("REMIX")) return false;
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
    private void ActionLoadLevel(string name)
    {
        var save = GameManager.save.game.levels.Find(level => level.levelID == name);

        // Loads the level
        LevelManager.Instance.LoadLevel(name);
        LevelManager.Instance.RefreshGameVars();
        LevelManager.Instance.RefreshGameUI();

        // Preload screen
        TransitionManager.Instance.ChangeTransition(Triangle);
        if (!LevelManager.Instance.currentLevel.hideUI) UI.Instance.preload.PreparePreloadScreen(save);
        else TransitionManager.Instance.TransitionOut<string>();
    }
}
