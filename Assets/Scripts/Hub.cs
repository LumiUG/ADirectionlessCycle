using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TransitionManager.Transitions;
using static Serializables;
using static GameTile;
using Coffee.UIEffects;
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
    public Text completedExtraText;
    public GameObject worldHolder;
    public RectTransform backgrounds;
    public RectTransform locks;
    public RectTransform outlineHolder;
    public RectTransform backButton;
    public GameObject masteryOutline;
    public Checker checker;
    public Text levelName;
    public Image orbFake;

    private readonly int[] positions = { 0, -2200, -4400, -6600 };
    private readonly List<int> completedLevelsCount = new() { 3, 3, 3, 0 };
    private readonly List<int> completedReal = new() { 0, 0, 0, 0 };
    private readonly List<int> completedRealRemix = new() { 0, 0, 0, 0 };
    private readonly List<int> completedRealOutbound = new() { 0, 0, 0, 0 };
    private readonly List<int> totalMainLevels = new() { 12, 12, 10, 1 };
    private readonly List<GameObject> remixList = new();
    private GameObject lastSelectedlevel = null;
    private Animator animator;
    private int worldIndex = 0;
    private bool delayOneFrame = false;

    private void Awake() => I = this; // No persistence!

    private void Start()
    {
        UI.I.selectors.ChangeSelected(backButton.gameObject, true);
        animator = GetComponent<Animator>();
 
        // Go back to the last world you selected.
        for (int shift = 0; shift < GameManager.I.lastSelectedWorld; shift++ ) ChangeWorld(1);

        // Unlock finale?
        GameManager.save.game.unlockedWorldSuper = GameManager.save.game.collectedOrbs.Count >= 3;

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
        else fragmentCountText.text = $"{GameManager.save.game.collectedFragments.Count}";
        completedCountText.text = $"{completedReal[worldIndex]}/{totalMainLevels[worldIndex]}";
        remixCountText.text = $"{completedRealRemix[worldIndex]}/{remixHolders[worldIndex].transform.childCount}";
        outboundCountText.text = $"{completedRealOutbound[0] + completedRealOutbound[1] + completedRealOutbound[2]}";
        MasteryEffect(0);

        // Extra levels (this'll be a pain for players :3)
        int masterExtras = ExtraLevelsCount();

        // Achievements
        if (completedReal[0] >= 12) GameManager.I.EditAchivement("ACH_COMPLETE_W1");
        if (completedReal[1] >= 12) GameManager.I.EditAchivement("ACH_COMPLETE_W2");
        if (completedReal[2] >= 10) GameManager.I.EditAchivement("ACH_COMPLETE_W3");

        // All main levels
        bool mainLevels = completedReal[0] >= 12 && completedReal[1] >= 12 && completedReal[2] >= 10;
        if (!GameManager.save.game.completedAllMainLevels && mainLevels)
        {
            GameManager.save.game.completedAllMainLevels = true;
            GameManager.I.EditAchivement("ACH_ALL_MAIN");
        }

        // All remix levels
        bool remixCount = completedRealRemix[0] + completedRealRemix[1] + completedRealRemix[2] >= 21;
        if (!GameManager.save.game.completedAllRemixLevels && remixCount)
        {
            GameManager.save.game.completedAllRemixLevels = true;
            GameManager.I.EditAchivement("ACH_ALL_INVERSE");
        }

        // All outbound levels
        bool outboundCount = completedRealOutbound[0] + completedRealOutbound[1] + completedRealOutbound[2] >= 8;
        if (!GameManager.save.game.completedAllOutboundLevels && outboundCount)
        {
            GameManager.save.game.completedAllOutboundLevels = true;
            GameManager.I.EditAchivement("ACH_ALL_OUTER");
        }
        
        // EVERYTHING.
        if (!GameManager.save.game.hasMasteredGame && mainLevels && remixCount && outboundCount)
        {
            if (!GameManager.save.game.hasCompletedGame) return;
            if (masterExtras < 6) return; // Hardcoded, edit if function is edited aswell.

            // Grant it, im not a monster.
            GameManager.save.game.hasMasteredGame = true;
            GameManager.I.EditAchivement("ACH_MASTERY");
        }
    }

    // Cycle through levels
    private void Update()
    {
        if (!EventSystem.current) return;

        if (delayOneFrame)
        {
            animator.Play("Reveal Top", 2);
            animator.Play("Reveal Bottom", 3);
            delayOneFrame = false;
        }

        if (EventSystem.current.currentSelectedGameObject == null) { UI.I.selectors.ChangeSelected(backButton.gameObject); return; }
        if (EventSystem.current.currentSelectedGameObject == backButton.gameObject || EventSystem.current.currentSelectedGameObject.name == "Unlock Button") {
            remixList.ForEach(item => item.SetActive(false));
            HideRevealUI(false);
            return;
        }

        // Checking if you swapped levels (condition)
        if (lastSelectedlevel == EventSystem.current.currentSelectedGameObject
        || !EventSystem.current.currentSelectedGameObject.transform.parent.name.StartsWith("W")) return;
        lastSelectedlevel = EventSystem.current.currentSelectedGameObject;
        
        // Update UI
        string levelID = $"{lastSelectedlevel.transform.parent.name}/{lastSelectedlevel.name}";
        if (levelID.Contains(".")) levelID = $"REMIX/{lastSelectedlevel.name.Split("-")[1]}";

        SerializableLevel level = LevelManager.I.GetLevel(levelID, false, true);
        PreviewText(levelID);

        // Show proper remix levels attached
        if (!levelID.Contains("REMIX"))
        {
            if (GameManager.save.game.mechanics.hasSeenRemix || GameManager.I.IsDebug())
            {
                if (level == null) HideRevealUI(false, false);
                else if (string.IsNullOrEmpty(level.remixLevel)) HideRevealUI(false, false, 0);
                else HideRevealUI(false, false, "654321".Contains(lastSelectedlevel.name.Split("-")[1]) ? 1 : 2); // 2:1 is oppsite rows btw
            }
            
            animator.Play("Blank", 2);
            animator.Play("Blank", 3);
        }

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
                var levelAsData = LevelManager.I.GetLevel(levelCheck.levelID, false, true);
                int displayCheck = HubCheck(levelAsData, levelCheck.levelID);

                // Add 1 to the completed level count (if not remix)
                if (OutboundCheck(levelAsData, levelCheck.levelID, true)) completedRealOutbound[index]++;
                if (!isRemix) {
                    if (completedLevelsCount[index] < totalMainLevels[index]) completedLevelsCount[index]++;
                    completedReal[index]++;
                } else completedRealRemix[index]++;

                // Check for the correct outline to use
                Image outlineImg = outline.GetComponent<Image>();
                if (GameManager.save.game.mechanics.hasSeenRemix && displayCheck == 1 && GameManager.save.preferences.missingHighlighter) outlineImg.color = GameManager.I.remixColor;
                else if (GameManager.save.game.mechanics.hasSwapUpgrade && displayCheck == 2 && GameManager.save.preferences.missingHighlighter) outlineImg.color = GameManager.I.outboundColor;
                else outlineImg.color = GameManager.I.completedColor; // for remixes!
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
                if (LevelManager.I.GetLevel($"{holder.name}/{child.name}", false, true) == null) return;
                child.GetComponent<Image>().color = Color.white;
            }
        }
    }

    // Hides/unhides some UI elements while a valid level is selected (not hovered)
    private void HideRevealUI(bool toggle, bool arrows = true, int overwrite = -1)
    {
        int remixSelection;
        if (lastSelectedlevel) remixSelection = "654321".Contains(lastSelectedlevel.name.Split("-")[1]) ? 2 : 1;
        else remixSelection = 0;

        if (overwrite != -1) remixSelection = overwrite;

        // Toggle on
        if (toggle)
        {
            if (remixSelection == 2 || remixSelection == 0) animator.Play("Away Top", 0);
            if (remixSelection == 1 || remixSelection == 0) animator.Play("Away Bottom", 1);
            if (arrows) animator.Play("Arrows In", 5);
            return;
        }

        // Toggle off
        if (remixSelection == 2 || remixSelection == 0) animator.Play("Revert Top", 0);
        if (remixSelection == 1 || remixSelection == 0) animator.Play("Revert Bottom", 1);
        if (arrows) animator.Play("Arrows Out", 5);
    }

    private void SetupLocks()
    {
        if (GameManager.I.IsDebug()) UI.I.global.SendMessage("(Hub debug unlock)", 2f);

        // World 2
        bool spikes = false;
        Transform wLock = locks.Find("W2");
        if (!GameManager.save.game.unlockedWorldTwo && !GameManager.I.IsDebug())
        {
            wLock.Find("Amount").GetComponent<Text>().text = $"{completedReal[0]}/9";
            foreach (Transform level in worldHolders[1].transform)
                { level.GetComponent<Button>().interactable = false; }
        }
        else {
            spikes = true;
            wLock.gameObject.SetActive(false);
        }

        // World 3
        wLock = locks.Find("W3");
        if (spikes) { wLock.Find("Spikes").gameObject.SetActive(true); wLock.Find("Filler").gameObject.SetActive(false); }
        if (!GameManager.save.game.unlockedWorldThree && !GameManager.I.IsDebug())
        {
            wLock.Find("Amount").GetComponent<Text>().text = $"{completedReal[1]}/9";
            foreach (Transform level in worldHolders[2].transform)
                { level.GetComponent<Button>().interactable = false; }
        } else wLock.gameObject.SetActive(false);

        // add debug later please / no i wont im lazy
        wLock = locks.Find("VOID");
        if (!GameManager.save.game.unlockedWorldSuper) 
        {
            Sprite spr = Resources.Load<Sprite>("Sprites/OrbDisabled");
            Transform o1 = wLock.Find("Orb 1");
            Transform o2 = wLock.Find("Orb 2");
            Transform o3 = wLock.Find("Orb 3");

            if (!GameManager.save.game.collectedOrbs.Contains("ORB/Orb One")) { o1.GetComponent<Image>().sprite = spr; orbFake.fillAmount += 0.33f; o1.GetComponent<UIEffect>().enabled = false; }
            if (!GameManager.save.game.collectedOrbs.Contains("ORB/Orb Two")) { o2.GetComponent<Image>().sprite = spr; orbFake.fillAmount += 0.33f; o2.GetComponent<UIEffect>().enabled = false; }
            if (!GameManager.save.game.collectedOrbs.Contains("ORB/Orb Three")) { o3.GetComponent<Image>().sprite = spr; orbFake.fillAmount += 0.33f; o3.GetComponent<UIEffect>().enabled = false; }
        }
    }

    // Now as a function for mouse hovers!
    public void PreviewText(string levelID)
    {
        if (levelID == "VOID/END") return;

        // Set the preview text
        SerializableLevel level = LevelManager.I.GetLevel(levelID, false, true);
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
        if (!LevelManager.I || TransitionManager.I.inTransition) return;
        if (!GameManager.save.game.unlockedWorldSuper && levelName == "VOID/END") { AudioManager.I.PlaySFX(AudioManager.uiDeny, 0.25f); return; }

        // Is the level locked?
        if (AbsurdLockedLevelDetection(levelName)) { AudioManager.I.PlaySFX(AudioManager.uiDeny, 0.25f); return; }

        // Plays the transition
        GameManager.I.lastSelectedWorld = worldIndex;
        TransitionManager.I.TransitionIn(Reveal, Actions.LoadLevel, levelName);
    }

    // Change world
    public void ChangeWorld(int direction)
    {
        if (EventSystem.current == null) return;
        if (worldIndex + direction >= positions.Length || worldIndex + direction < 0) return;
        
        // Stuff for super world.
        if (worldIndex + direction == 3)
        {
            if (!GameManager.save.game.mechanics.hasSwapUpgrade) return;
            GameObject.Find("FINALE").GetComponent<Button>().interactable = true;
        }

        // Move!!! (animation, i know im repeating two switches.)
        switch (worldIndex)
        {
            case 0:
                if (direction > 0) animator.Play("W1Right", 4);
                else animator.Play("W1Left", 4);
                break;
            case 1:
                if (direction > 0) animator.Play("W2Right", 4);
                else animator.Play("W2Left", 4);
                break;
            case 2:
                if (direction > 0) animator.Play("W3Right", 4);
                else animator.Play("W3Left", 4);
                break;
            case 3:
                if (direction < 0) animator.Play("WSLeft", 4);
                break;
            default:
                break;
        }
        worldIndex += direction;

        // Disable arrows, etc
        switch (worldIndex)
        {
            case 2:
                if (GameManager.save.game.mechanics.hasSwapUpgrade) { hubArrows[1].interactable = true; break; };
                UI.I.selectors.ChangeSelected(backButton.gameObject);
                hubArrows[1].interactable = false;
                break;
            case 3:
                UI.I.selectors.ChangeSelected(backButton.gameObject);
                hubArrows[1].interactable = false;
                break;
            case 0:
                UI.I.selectors.ChangeSelected(backButton.gameObject);
                hubArrows[0].interactable = false;
                break;
            default:
                hubArrows[0].interactable = true;
                hubArrows[1].interactable = true;
                break;
        }

        // Change display
        switch (worldIndex) 
        {
            case 0: SetLevelName("Area 1"); break;
            case 1: SetLevelName("Area 2"); break;
            case 2: SetLevelName("Area 3"); break;
            case 3: SetLevelName("The Core"); break;
        }

        // Update world completions
        if (worldIndex <= 2)
        {
            completedCountText.text = $"{completedReal[worldIndex]}/{totalMainLevels[worldIndex]}";
            remixCountText.text = $"{completedRealRemix[worldIndex]}/{remixHolders[worldIndex].transform.childCount}";
            // outboundCountText.text = $"{completedRealOutbound[worldIndex]}";
        } else {
            completedCountText.text = "?????";
            remixCountText.text = "?????";
        }

        // Update ui
        MasteryEffect(worldIndex);
        checker.dirX = direction;
        
        if (EventSystem.current.currentSelectedGameObject == hubArrows[0].gameObject || EventSystem.current.currentSelectedGameObject == hubArrows[1].gameObject) return;
        if (hubArrows[0].interactable && hubArrows[1].interactable) UI.I.selectors.ChangeSelected(backButton.gameObject, true);
    }

    // Returns true if a level is locked. (FALSE = good)
    public bool AbsurdLockedLevelDetection(string fullLevelID)
    {
        if (LevelManager.I.GetLevel(fullLevelID, false, true) == null) return true;
        if (GameManager.I.IsDebug()) return false;
        if (fullLevelID == "VOID/END") return false;

        // Custom handling for remix levels
        if (fullLevelID.StartsWith("REMIX/")) return GameManager.save.game.levels.Find(l => l.levelID == fullLevelID) == null;

        string[] levelSplit = fullLevelID.Split("/")[1].Split("-");
        return completedLevelsCount[int.Parse(levelSplit[0]) - 1] < int.Parse(levelSplit[1]);
    }

    // yeah
    private void RemixUIChecks(SerializableLevel level, string levelID)
    {
        if (levelID.Contains("REMIX") || level == null) return;
        if (!GameManager.I.IsDebug() && !GameManager.save.game.mechanics.hasSeenRemix) return;

        remixList.ForEach(item => item.SetActive(false));
        remixList.Clear();

        if (level.levelName == "Seeing Double") { HideRevealUI(false); return; }
        if (GameManager.save.game.levels.Find(l => l.levelID == level.remixLevel) != null || GameManager.I.IsDebug())
        {
            if (string.IsNullOrEmpty(level.remixLevel)) return;
            HideRevealUI(true);
            UIRecursiveRemixes(level.remixLevel, levelID, 1);
        }
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
        if (string.IsNullOrEmpty(level.remixLevel)) return 0;

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

            // Show remix level animation
            delayOneFrame = true;
            
            // Toggles outline on
            if (outline)
            {
                remixList.Add(outline.gameObject);
                outline.gameObject.SetActive(true);
            }
        }

        // We jump to the next level, if current level has a remix level.
        SerializableLevel current = LevelManager.I.GetLevel(remix, false, true);
        if (current == null) return;
        if (!string.IsNullOrEmpty(current.remixLevel) && GameManager.save.game.levels.Find(l => l.levelID == current.remixLevel) != null) UIRecursiveRemixes(current.remixLevel, level, count + 1);
        else if (GameManager.I.IsDebug() && !string.IsNullOrEmpty(current.remixLevel)) UIRecursiveRemixes(current.remixLevel, level, count + 1);
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
        Transform nextWorld;
        switch (index)
        {
            case 1:
                if (completedLevelsCount[0] < 12) return;
                GameManager.save.game.unlockedWorldTwo = true;
                locks.Find("W2").gameObject.SetActive(false);

                nextWorld = locks.Find("W3");
                nextWorld.Find("Spikes").gameObject.SetActive(true);
                nextWorld.Find("Filler").gameObject.SetActive(false);
                break;
            case 2:
                if (completedLevelsCount[1] < 12) return;
                GameManager.save.game.unlockedWorldThree = true;
                locks.Find("W3").gameObject.SetActive(false);
                break;
            default:
                return;
        }

        // Reactivate buttons
        UI.I.selectors.ChangeSelected(backButton.gameObject);
        foreach (Transform level in worldHolders[index].transform) { level.GetComponent<Button>().interactable = true; }
    }

    internal void MasteryEffect(int world)
    {
        if (
            (completedReal[0] >= 12 && completedRealRemix[0] >= 10 && completedRealOutbound[0] >= 1 && world == 0) ||
            (completedReal[1] >= 12 && completedRealRemix[1] >= 7 && completedRealOutbound[1] >= 4 && world == 1) ||
            (completedReal[2] >= 10 && completedRealRemix[2] >= 4 && completedRealOutbound[2] >= 3 && world == 2)
        ) masteryOutline.SetActive(true);
        else masteryOutline.SetActive(false);
    }

    private int ExtraLevelsCount()
    {
        int extraWins = 0;
        GameData.Level[] list = {
            GameManager.save.game.levels.Find(level => level.levelID == $"ORB/Orb One"),
            GameManager.save.game.levels.Find(level => level.levelID == $"ORB/Orb Two"),
            GameManager.save.game.levels.Find(level => level.levelID == $"ORB/Orb Three"),
            GameManager.save.game.levels.Find(level => level.levelID == $"FRAGMENTS/Fragment Two"),
            GameManager.save.game.levels.Find(level => level.levelID == $"FRAGMENTS/Tutorial"),
            GameManager.save.game.levels.Find(level => level.levelID == $"REMIX/Meem")
        };
        foreach (var level in list)
        {
            if (level == null) continue;
            if (level.completed || level.outboundCompletion) extraWins++;
        };
        if (extraWins > 0)
        {
            completedExtraText.gameObject.SetActive(true);
            completedExtraText.text = $"+{extraWins}";
        }
        return extraWins;
    }
}
