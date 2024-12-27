using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TransitionManager.Transitions;
using static Serializables;
using static GameTile;

public class Hub : MonoBehaviour
{
    [HideInInspector] public static Hub I;
    public List<GameObject> worldHolders = new(capacity: 3);
    public List<GameObject> remixHolders = new(capacity: 3);
    public List<Button> hubArrows = new(capacity: 2);
    public Text completedCountText;
    public Text fragmentCountText;
    public GameObject worldHolder;
    public RectTransform outlineHolder;
    public RectTransform backButton;
    public Checker checker;
    public Text levelName;

    private readonly int[] positions = { 0, -2200, -4400, -6600 };
    private readonly List<int> completedLevelsCount = new() { 3, 3, 3, 0 };
    private readonly List<int> completedReal = new() { 0, 0, 0, 0 };
    private readonly List<GameObject> remixList = new();
    private Color remixColor;
    private Color outboundColor;
    private Color completedColor;
    private GameObject lastSelectedlevel = null;
    private RectTransform holderRT = null;
    private Animator animator;
    private int worldIndex = 0;

    private void Awake()
    {
        I = this; // No persistence!
    }

    private void Start()
    {
        UI.Instance.selectors.ChangeSelected(backButton.gameObject, true);
        holderRT = worldHolder.GetComponent<RectTransform>();
        animator = GetComponent<Animator>();

        // Colors!!
        ColorUtility.TryParseHtmlString("#E5615F", out remixColor);
        ColorUtility.TryParseHtmlString("#A22BE3", out outboundColor);
        ColorUtility.TryParseHtmlString("#4CF832", out completedColor);

        // Iterate all non-remix levels.
        for (int count = 0; count < worldHolders.Count; count++) { PrepareHub(worldHolders[count], false, count); }

        // Iterate all remix levels.
        for (int count = 0; count < remixHolders.Count; count++) { PrepareHub(remixHolders[count], true, count); }

        // Initial variables!
        if (GameManager.save.game.collectedFragments.Count <= 0) fragmentCountText.gameObject.SetActive(false);
        else fragmentCountText.text = $"{GameManager.save.game.collectedFragments.Count}/3";
        completedCountText.text = $"{completedReal[worldIndex]}/12";
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
        PreviewText(levelID);

        // Show proper remix levels attached
        RemixUIChecks(level, levelID);
    }

    // Prepares the hub and outlines
    private void PrepareHub(GameObject holder, bool isRemix, int index)
    {
        // Check all levels present in the hub for completion
        for (int childnNum = 0; childnNum < holder.transform.childCount; childnNum++)
        {
            Transform child = holder.transform.GetChild(childnNum);
            GameData.Level levelCheck;
            string cname = child.name;

            // Find the level
            if (isRemix) {
                cname = cname.Split("-")[1];
                levelCheck = GameManager.save.game.levels.Find(level => level.levelID == $"REMIX/{cname}");
            } else {
                levelCheck = GameManager.save.game.levels.Find(level => level.levelID == $"{holder.name}/{cname}");
            }

            // Level doesnt exist? Leave.
            if (levelCheck == null) continue;

            // Add an outline to the level
            if (levelCheck.completed)
            {
                Transform outline;
                if (isRemix) outline = outlineHolder.Find("REMIX").Find(holder.name).GetChild(childnNum);
                else {
                    outline = outlineHolder.Find(holder.name).Find(cname);
                    outline.gameObject.SetActive(true);
                }
                
                // Add 1 to the completed level count (if not remix)
                if (!isRemix) {
                    if (completedLevelsCount[index] < 12) completedLevelsCount[index]++;
                    completedReal[index]++;
                }

                // Get level data and check for the correct outline to use
                var levelAsData = LevelManager.Instance.GetLevel(levelCheck.levelID, false, true);
                int displayCheck = HubCheck(levelAsData, levelCheck.levelID);

                Image outlineImg = outline.GetComponent<Image>();
                if (GameManager.save.game.mechanics.hasSeenRemix && displayCheck == 1) outlineImg.color = remixColor;
                else if (GameManager.save.game.mechanics.hasSwapUpgrade && displayCheck == 2) outlineImg.color = outboundColor;
                else outlineImg.color = completedColor; // for remixes!
            }
        }

        // Progress locking (between worlds, not levels)
        if (completedLevelsCount[0] < 12)
        {
            completedLevelsCount[1] = 0;
            completedLevelsCount[2] = 0;
        }
        else if (completedLevelsCount[1] < 12) { completedLevelsCount[2] = 0; }

        // nuh uh
        if (holder.transform.parent.name == "REMIX") return;

        // Setting level color for available hub levels
        for (int j = 0; j < completedLevelsCount[index]; j++)
        { 
            Transform child = holder.transform.GetChild(j);
            if (child)
            {
                if (LevelManager.Instance.GetLevel($"{holder.name}/{child.name}", false, true) == null) return;
                child.GetComponent<Image>().color = Color.white;
            }
        }
    }

    // Hides/unhides some UI elements while a valid level is selected (not hovered)
    private void HideRevealUI(bool toggle)
    {
        // Toggle on
        if (toggle)
        {
            animator.Play("Away", 0);
            return;
        }

        // Toggle off
        animator.Play("Revert");
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

            // Also show other levels (if applicable)
            // RemixUIChecks(level, levelID);
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
        if (worldIndex + direction == 3 && GameManager.save.game.collectedOrbs.Count < 1) return;

        // if (!GameManager.Instance.IsDebug()) {
        //     if ((worldIndex + direction == 1 && completedLevelsCount[1] < 1) || (worldIndex + direction == 2 && completedLevelsCount[2] < 1)) return;
        //     if (worldIndex + direction == 3 && GameManager.save.game.collectedOrbs.Count < 1) return;
        // }

        // Move!!!
        worldIndex += direction;
        holderRT.anchoredPosition = new(positions[worldIndex], holderRT.anchoredPosition.y);
        outlineHolder.anchoredPosition = new(positions[worldIndex], holderRT.anchoredPosition.y);

        // Disable arrows, etc
        switch (worldIndex)
        {
            case 2:
                if (GameManager.save.game.collectedOrbs.Count >= 1) break;
                UI.Instance.selectors.ChangeSelected(backButton.gameObject);
                hubArrows[1].interactable = false;
                break;
            case 3:
                UI.Instance.selectors.ChangeSelected(backButton.gameObject);
                hubArrows[1].interactable = false;
                break;
            case 0:
                UI.Instance.selectors.ChangeSelected(backButton.gameObject);
                hubArrows[0].interactable = false;
                break;
            default:
                hubArrows[0].interactable = true;
                hubArrows[1].interactable = true;
                break;
        }

        // Update world completions
        completedCountText.text = $"{completedReal[worldIndex]}/12";

        // Update checker direction
        checker.dirX = direction;
        
        if (EventSystem.current.currentSelectedGameObject == hubArrows[0].gameObject || EventSystem.current.currentSelectedGameObject == hubArrows[1].gameObject) return;
        if (hubArrows[0].interactable && hubArrows[1].interactable) UI.Instance.selectors.ChangeSelected(backButton.gameObject, true);
    }

    // Returns true if a level is locked. (FALSE = good)
    public bool AbsurdLockedLevelDetection(string fullLevelID)
    {
        if (LevelManager.Instance.GetLevel(fullLevelID, false, true) == null) return true;
        if (GameManager.Instance.IsDebug()) return false;

        // Custom handling for remix levels
        if (fullLevelID.StartsWith("REMIX/")) return GameManager.save.game.levels.Find(l => l.levelID == fullLevelID) == null;

        string[] levelSplit = fullLevelID.Split("/")[1].Split("-");
        return completedLevelsCount[int.Parse(levelSplit[0]) - 1] < int.Parse(levelSplit[1]);
    }

    // yeah
    private void RemixUIChecks(SerializableLevel level, string levelID)
    {
        if (levelID.Contains("REMIX") || level == null) return;
        if (!GameManager.Instance.IsDebug() && !GameManager.save.game.mechanics.hasSeenRemix) return;
        
        remixList.ForEach(item => item.SetActive(false));
        // animator.Play("Blank", 1);
        // animator.Play("Blank", 2);
        remixList.Clear();

        if (!LevelManager.Instance.IsStringEmptyOrNull(level.remixLevel))
        {
            HideRevealUI(true); 
            UIRecursiveRemixes(level.remixLevel, levelID, 1);
        }
        else HideRevealUI(false);
    }

    // 0 = green (ignore)
    // 1 = red
    // 2 = purple
    private int HubCheck(SerializableLevel level, string levelID)
    {
        if (level == null) return 0;

        // Outerbound?
        if (level.tiles.overlapTiles.Exists(t => { return t.type == ObjectTypes.OutboundArea.ToString(); })
            && GameManager.save.game.levels.Exists(l => { return l.levelID == levelID && l.outboundCompletion == false; })) return 2;

        // Remix completion?
        if (LevelManager.Instance.IsStringEmptyOrNull(level.remixLevel)) return 0;

        GameData.Level statCheck = GameManager.save.game.levels.Find(l => l.levelID == level.remixLevel);
        if (statCheck == null) return 1;
        return 0; // fallback
    }

    // recursion bullshit here
    private void UIRecursiveRemixes(string remix, string level, int count)
    {
        string world = level.Split("/")[0];
        string fullName = $"{level.Split("-")[1]}.{count}-{remix.Replace("REMIX/", "")}";
        Transform selected = worldHolder.transform.Find("REMIX").Find(world).Find(fullName);
        Transform outline = outlineHolder.transform.Find("REMIX").Find(world).Find(fullName);

        if (selected)
        {
            remixList.Add(selected.gameObject);
            selected.gameObject.SetActive(true);
            // animator.Play("Reveal Top", 1);
            // animator.Play("Reveal Bottom", 2);
            
            if (outline)
            {
                remixList.Add(outline.gameObject);
                outline.gameObject.SetActive(true);
            }
        }

        // Next!
        SerializableLevel current = LevelManager.Instance.GetLevel(remix, false, true);
        if (!LevelManager.Instance.IsStringEmptyOrNull(current.remixLevel)) UIRecursiveRemixes(current.remixLevel, level, count + 1);
    }

    // Actions //
}
