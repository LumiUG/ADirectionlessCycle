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
    public List<GameObject> remixHolders = new(capacity: 3);
    public List<Button> hubArrows = new(capacity: 2);
    public Text completedCountText;
    public Text remixCountText;
    public Text outboundCountText;
    public Text fragmentCountText;
    public GameObject worldHolder;
    public RectTransform backgrounds;
    public RectTransform locks;
    public RectTransform outlineHolder;
    public RectTransform backButton;
    public Checker checker;
    public Text levelName;

    private readonly int[] positions = { 0, -2200, -4400, -6600 };
    private readonly List<int> completedLevelsCount = new() { 3, 3, 3, 0 };
    private readonly List<int> completedReal = new() { 0, 0, 0, 0 };
    private readonly List<int> completedRealRemix = new() { 0, 0, 0, 0 };
    private readonly List<int> completedRealOutbound = new() { 0, 0, 0, 0 };
    private readonly List<GameObject> remixList = new();
    private Color remixColor;
    private Color outboundColor;
    private Color completedColor;
    private GameObject lastSelectedlevel = null;
    private Animator animator;
    private int worldIndex = 0;

    private void Awake()
    {
        I = this; // No persistence!
    }

    private void Start()
    {
        UI.Instance.selectors.ChangeSelected(backButton.gameObject, true);
        animator = GetComponent<Animator>();

        // Colors!!
        ColorUtility.TryParseHtmlString("#E5615F", out remixColor);
        ColorUtility.TryParseHtmlString("#A22BE3", out outboundColor);
        ColorUtility.TryParseHtmlString("#4CF832", out completedColor);

        // Iterate all non-remix levels.
        for (int count = 0; count < worldHolders.Count; count++) { PrepareHub(worldHolders[count], false, count); }

        // Iterate all remix levels.
        for (int count = 0; count < remixHolders.Count; count++) { PrepareHub(remixHolders[count], true, count); }

        // Lock screen for levels
        SetupLocks();

        // Initial variables!
        if (!GameManager.save.game.mechanics.hasSeenRemix) remixCountText.gameObject.SetActive(false);
        if (!GameManager.save.game.mechanics.hasSwapUpgrade) outboundCountText.gameObject.SetActive(false);
        if (GameManager.save.game.collectedFragments.Count <= 0) fragmentCountText.gameObject.SetActive(false);
        else fragmentCountText.text = $"{GameManager.save.game.collectedFragments.Count}/3";
        completedCountText.text = $"{completedReal[worldIndex]}/12";
        remixCountText.text = $"{completedRealRemix[worldIndex]}/{remixHolders[worldIndex].transform.childCount}";
        outboundCountText.text = $"{completedRealOutbound[worldIndex]}/?";
    }

    // Cycle through levels
    private void Update()
    {
        if (!EventSystem.current) return;
        if (EventSystem.current.currentSelectedGameObject == null) return;
        if (EventSystem.current.currentSelectedGameObject == backButton.gameObject || EventSystem.current.currentSelectedGameObject.name == "Unlock Button") {
            remixList.ForEach(item => item.SetActive(false));
            HideRevealUI(false);
            levelName.text = "";
            return;
        }

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

                // Get level data
                var levelAsData = LevelManager.Instance.GetLevel(levelCheck.levelID, false, true);
                int displayCheck = HubCheck(levelAsData, levelCheck.levelID);

                // Add 1 to the completed level count (if not remix)
                if (OutboundCheck(levelAsData, levelCheck.levelID, true)) completedRealOutbound[index]++;
                if (!isRemix) {
                    if (completedLevelsCount[index] < 12) completedLevelsCount[index]++;
                    completedReal[index]++;
                } else completedRealRemix[index]++;

                // Check for the correct outline to use
                Image outlineImg = outline.GetComponent<Image>();
                if (GameManager.save.game.mechanics.hasSeenRemix && displayCheck == 1) outlineImg.color = remixColor;
                else if (GameManager.save.game.mechanics.hasSwapUpgrade && displayCheck == 2) outlineImg.color = outboundColor;
                else outlineImg.color = completedColor; // for remixes!
            }
        }
        
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

    private void SetupLocks()
    {
        if (GameManager.Instance.IsDebug()) UI.Instance.global.SendMessage("(Hub debug unlock)");

        // World 2
        Transform wLock = locks.Find("W2");
        if (!GameManager.save.game.unlockedWorldTwo && !GameManager.Instance.IsDebug())
        {
            wLock.Find("Amount").GetComponent<Text>().text = $"{completedReal[0]}/9";
            foreach (Transform level in worldHolders[1].transform)
                { level.GetComponent<Button>().interactable = false; }
        }
        else wLock.gameObject.SetActive(false);

        // World 3
        wLock = locks.Find("W3");
        if (!GameManager.save.game.unlockedWorldThree && !GameManager.Instance.IsDebug())
        {
            wLock.Find("Amount").GetComponent<Text>().text = $"{completedReal[1]}/9";
            foreach (Transform level in worldHolders[2].transform)
                { level.GetComponent<Button>().interactable = false; }
        }
        else wLock.gameObject.SetActive(false);

        // add debug later please
        if (!GameManager.save.game.unlockedWorldSuper) Debug.Log("Not yet! (SW)");
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
        
        // Stuff for super world.
        var level = GameManager.save.game.levels.Find(level => level.levelID == "W3/3-12");
        if (worldIndex + direction == 3) if (level != null) if (!level.completed) return;

        // Move!!! (animation, i know im repeating two switches.)
        switch (worldIndex)
        {
            case 0:
                if (direction > 0) animator.Play("W1Right", 3);
                else animator.Play("W1Left", 3);
                break;
            case 1:
                if (direction > 0) animator.Play("W2Right", 3);
                else animator.Play("W2Left", 3);
                break;
            case 2:
                if (direction > 0) animator.Play("W3Right", 3);
                else animator.Play("W3Left", 3);
                break;
            case 3:
                if (direction < 0) animator.Play("WSLeft", 3);
                break;
            default:
                break;
        }
        worldIndex += direction;

        // Disable arrows, etc
        switch (worldIndex)
        {
            case 2:
                if (level != null) if (level.completed) { hubArrows[1].interactable = true; break; };
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
        if (worldIndex <= 2)
        {
            remixCountText.text = $"{completedRealRemix[worldIndex]}/{remixHolders[worldIndex].transform.childCount}";
            outboundCountText.text = $"{completedRealOutbound[worldIndex]}/?";
        }

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

        if (GameManager.save.game.levels.Find(l => l.levelID == level.remixLevel) != null || GameManager.Instance.IsDebug())
        {
            if (LevelManager.Instance.IsStringEmptyOrNull(level.remixLevel)) return;
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
        if (OutboundCheck(level, levelID)) return 2;

        // Remix completion?
        if (LevelManager.Instance.IsStringEmptyOrNull(level.remixLevel)) return 0;

        GameData.Level statCheck = GameManager.save.game.levels.Find(l => l.levelID == level.remixLevel);
        if (statCheck == null) return 1;
        return 0; // fallback
    }

    // recursion bullshit here
    private void UIRecursiveRemixes(string remix, string level, int count)
    {
        // Find all references for level/outlines/etc
        string world = level.Split("/")[0];
        string fullName = $"{level.Split("-")[1]}.{count}-{remix.Replace("REMIX/", "")}";
        Transform selected = worldHolder.transform.Find("REMIX").Find(world).Find(fullName);
        Transform outline = outlineHolder.transform.Find("REMIX").Find(world).Find(fullName);

        // Toggles level on
        if (selected)
        {
            remixList.Add(selected.gameObject);
            selected.gameObject.SetActive(true);
            // animator.Play("Reveal Top", 1);
            // animator.Play("Reveal Bottom", 2);
            
            // Toggles outline on
            if (outline)
            {
                remixList.Add(outline.gameObject);
                outline.gameObject.SetActive(true);
            }
        }

        // We jump to the next level, if current level has a remix level.
        SerializableLevel current = LevelManager.Instance.GetLevel(remix, false, true);
        if (!LevelManager.Instance.IsStringEmptyOrNull(current.remixLevel) && GameManager.save.game.levels.Find(l => l.levelID == current.remixLevel) != null) UIRecursiveRemixes(current.remixLevel, level, count + 1);
        else if (GameManager.Instance.IsDebug() && !LevelManager.Instance.IsStringEmptyOrNull(current.remixLevel)) UIRecursiveRemixes(current.remixLevel, level, count + 1);
    }

    // Check for outbounds on a completed level
    private bool OutboundCheck(SerializableLevel level, string levelID, bool completed = false)
    {
        return level.tiles.overlapTiles.Exists(t => { return t.type == ObjectTypes.OutboundArea.ToString(); })
            && GameManager.save.game.levels.Exists(l => { return l.levelID == levelID && l.outboundCompletion == completed; });
    }

    // Unlocks a world, setting a variable to your savefile for easy access
    public void UnlockWorld(int index)
    {
        switch (index)
        {
            case 1:
                if (completedLevelsCount[0] < 12) return;
                GameManager.save.game.unlockedWorldTwo = true;
                locks.Find("W2").gameObject.SetActive(false);
                break;
            case 2:
                if (completedLevelsCount[1] < 12) return;
                GameManager.save.game.unlockedWorldThree = true;
                locks.Find("W3").gameObject.SetActive(false);
                break;
            case 3:
                Debug.Log("Not yet unlockeable.");
                break;

            default:
                return;
        }

        // Reactivate buttons
        UI.Instance.selectors.ChangeSelected(backButton.gameObject);
        foreach (Transform level in worldHolders[index].transform) { level.GetComponent<Button>().interactable = true; }
    }

    // Actions //
}
