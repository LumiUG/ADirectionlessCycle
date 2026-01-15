using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;
using static GameTile;

public class DialogManager : MonoBehaviour
{
    [HideInInspector] public static DialogManager I;
    public LocalizedStringDatabase dialogStrings;

    public DialogScriptable loadedDial;
    public string[] dialog;
    public DialogEvent[] events = { new() };
    public float textSpeed = 0.05f;
    public bool canBeSkipped = true;
    public bool shouldExhaust = true;
    public bool inDialog = false;

    private readonly string[] waitExtraSmall = { ",", "-" }; // "\""
    private readonly string[] waitExtraMedium = { ".", ":", ";" };
    private readonly string[] waitExtraLong = { "!", "?" }; // ":", ";"
    private bool ignoreNewChatSource;
    private bool hasDialogStarted;
    private bool canInteract;
    private int dialogIndex;
    private string currentDialogPath;

    public void Awake()
    {
        // Singleton (DialogManager has persistence)
        if (!I) { I = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        ResetPrivatesToDefaultState();
        inDialog = false;
    }

    // Starts the dialog with the NPC
    public void StartDialog(DialogScriptable chat, string dialogPath, bool doLocal = false)
    {
        if (!canInteract || !chat || TransitionManager.I.inTransition) return;

        var changes = CustomHandlings(chat, dialogPath);
        if (changes.Item1 != null) chat = changes.Item1;
        if (changes.Item2 != null) dialogPath = changes.Item2;
        currentDialogPath = dialogPath;
        inDialog = true;

        // Check if we should use the exhausted dialog instead
        while (GameManager.save.game.exhaustedDialog.Contains(dialogPath))
        {
            chat = chat.exhaustDialog;
            dialogPath = $"EXHAUST-{dialogPath}";
            currentDialogPath = dialogPath;
        }

        // Should we change/load the new scriptable?
        if (!chat) { inDialog = false; return; }
        if (!loadedDial || chat != loadedDial && !ignoreNewChatSource) DelegateScriptable(chat);

        // Translate strings before serving dialog
        if (doLocal)
        {
            var localizedStr = LocalizationSettings.StringDatabase.GetLocalizedString("Dialog", "test");
            chat.dialog[dialogIndex] = localizedStr;
        }

        // Not the first time?
        if (hasDialogStarted) { ProceedChat(); return; }

        // Toggles the dialog box on
        UI.I.dialog.Toggle(true);
    
        // Resets the dialog box
        UI.I.dialog.text.text = string.Empty;
        hasDialogStarted = true;

        // Reads a line
        NextLine();
    }

    private void ProceedChat(bool forceNext = false)
    {
        // Go to the next line of dialog
        if (UI.I.dialog.text.text == dialog[dialogIndex] || forceNext) { dialogIndex++; NextLine(); return; }

        // Stop showing text and show it all instantly
        if (!canBeSkipped) return;
        ShowEntireLine();
    }

    // Changes dialog line to a next one or ends the conversation
    private void NextLine()
    {
        // Executes all dialog events
        foreach (DialogEvent ev in events)
        {
            if (ev.executeAtIndex == dialogIndex)
            {
                ev.textSpeedEvent.Run();
                ev.setTileEvent.Run();
                ev.loadLevelEvent.Run();
            }
        }

        // Goes to the next line of dialog
        if (dialogIndex < dialog.Length)
        { 
            // Reads the line
            StartCoroutine(ReadLine());
            return;
        }

        // Ends dialog
        UI.I.dialog.Toggle(false);
        ResetPrivatesToDefaultState();
        inDialog = false;

        // Has the chat ended? Exhaust the chat
        if (!shouldExhaust || LevelManager.I.currentLevelID == LevelManager.I.levelEditorName) return;
        if (!GameManager.save.game.exhaustedDialog.Contains(currentDialogPath)) GameManager.save.game.exhaustedDialog.Add(currentDialogPath);
    }

    // Reads a line of text
    public IEnumerator ReadLine()
    {
        // Resets dialogbox text
        UI.I.dialog.SetText(string.Empty);

        // Draws every character on the dialogbox
        foreach (char c in dialog[dialogIndex])
        {
            UI.I.dialog.SetText(c.ToString(), true);
            if (loadedDial.sfx != null) AudioManager.I.PlaySFX(loadedDial.sfx, 0.45f);
            if (waitExtraLong.Contains(c.ToString())) yield return new WaitForSecondsRealtime(textSpeed + 0.25f);
            if (waitExtraMedium.Contains(c.ToString())) yield return new WaitForSecondsRealtime(textSpeed + 0.175f);
            if (waitExtraSmall.Contains(c.ToString())) yield return new WaitForSecondsRealtime(textSpeed + 0.1f);
            else yield return new WaitForSecondsRealtime(textSpeed);
        }
    }

    // Change dialog scriptable (delegate or fucking whatever its called idk man)
    public void DelegateScriptable(DialogScriptable newDialog, bool doDefaults = false) {
        dialog = newDialog.dialog;
        events = newDialog.events;
        textSpeed = newDialog.textSpeed;
        canBeSkipped = newDialog.canBeSkipped;
        shouldExhaust = newDialog.shouldExhaust;
        loadedDial = newDialog;

        // Reset to defaults?
        if (!doDefaults) return;
        ResetPrivatesToDefaultState();
        ignoreNewChatSource = true;
        inDialog = true;
    }

    // Resets the private scriptable variables to its default state
    private void ResetPrivatesToDefaultState()
    {
        hasDialogStarted = false;
        canInteract = true;
        dialogIndex = 0;
        ignoreNewChatSource = false;
    }

    // Instantly shows the entire line
    private void ShowEntireLine()
    {
        UI.I.dialog.SetText(dialog[dialogIndex]);
        StopAllCoroutines();
    }

    // Custom handlings for secrets or oddities.
    private (DialogScriptable, string) CustomHandlings(DialogScriptable chat, string path)
    {
        // Postgame changes
        if (GameManager.save.game.hasCompletedGame && !path.Contains("CUSTOM"))
        {
            if (chat.events.Length == 0 && (chat.sfx == AudioManager.ego1 || chat.sfx == AudioManager.ego2))
            {
                if (chat.shouldExhaust && chat.exhaustDialog == null) return (null, null);
                chat = Resources.Load<DialogScriptable>("Dialog/Empty");
                path = "Empty";
                return (chat, path);
            }

            if (path == "Dialog/Orb Hub 1/Gus" && GameManager.save.game.exhaustedDialog.Contains("EXHAUST-EXHAUST-EXHAUST-EXHAUST-EXHAUST-EXHAUST-Dialog/Orb Hub 1/Gus"))
            {
                chat = Resources.Load<DialogScriptable>("Dialog/Orb Hub 1/Post/PG");
                path = "Dialog/Orb Hub 1/Post/PG";
                return (chat, path);
            }
        }

        // Fragments sequence breaking
        if (path == "Dialog/Orb Hub 1/Hint" && !GameManager.save.game.exhaustedDialog.Contains("Dialog/3-12/Light"))
        {
            chat = Resources.Load<DialogScriptable>("Dialog/SB/Frag 1");
            path = "Dialog/SB/Frag 1";
            return (chat, path);
        }
        if (path == "Dialog/Fragment 3/Enter" && !GameManager.save.game.exhaustedDialog.Contains("Dialog/3-12/Light"))
        {
            chat = Resources.Load<DialogScriptable>("Dialog/SB/Frag 3");
            path = "Dialog/SB/Frag 3";
            return (chat, path);
        }
        if (path == "Dialog/END/Fire") AudioManager.I.PlaySFX(AudioManager.tileDeath, 0.4f);

        return (null, null);
    }

    // Dialog events
    [Serializable]
    public class DialogEvent
    {
        public EventTextSpeed textSpeedEvent;
        public EventSetTile setTileEvent;
        public EventLoadLevel loadLevelEvent;
        public int executeAtIndex = 0;

        // Required to have Run() or something idk
        [Serializable] public abstract class EventAction {
            public bool enabled = false;
            public abstract void Run();
        }

        // Change text speed
        [Serializable]
        public class EventTextSpeed : EventAction
        {
            public float textSpeed;
            public override void Run()
            {
                if (!enabled) return;
                I.textSpeed = textSpeed;
            }
        }

        // Set a tile
        [Serializable]
        public class EventSetTile : EventAction
        {
            public ObjectTypes setAs = new();
            public Vector3Int position = new();
            public bool delete = false;
            public override void Run()
            {
                if (!enabled) return;

                // Delete a tile
                if (delete)
                {
                    GameTile tile;
                    tile = setAs switch
                    {
                        var t when LevelManager.I.typesSolidsList.Contains(t) => LevelManager.I.tilemapCollideable.GetTile<GameTile>(position),
                        var t when LevelManager.I.typesAreas.Contains(t) => LevelManager.I.tilemapWinAreas.GetTile<GameTile>(position),
                        var t when LevelManager.I.typesHazardsList.Contains(t) => LevelManager.I.tilemapHazards.GetTile<GameTile>(position),
                        var t when LevelManager.I.typesEffectsList.Contains(t) => LevelManager.I.tilemapEffects.GetTile<GameTile>(position),
                        var t when LevelManager.I.typesCustomsList.Contains(t) => LevelManager.I.tilemapCustoms.GetTile<GameTile>(position),
                        _ => LevelManager.I.tilemapObjects.GetTile<GameTile>(position),
                    };
                    if (tile) LevelManager.I.RemoveTile(tile);

                    // Deleting effect
                    // Debug.Log(position);
                    return;
                }

                // Place a tile (MEMORY LEAKS, FIX TODO)
                LevelManager.I.PlaceTile(LevelManager.I.CreateTile(setAs.ToString(), new(), position));

                // Placing effect
                // Debug.Log(position);
            }
        }


        // Change level
        [Serializable]
        public class EventLoadLevel : EventAction
        {
            public string levelID;
            public override void Run()
            {
                if (!enabled) return;

                // "same" code as Level.cs!!!
                var levelTest = LevelManager.I.LoadLevel(levelID);
                if (!levelTest) levelTest = LevelManager.I.LoadLevel(levelID, true);
                
                if (levelTest)
                {
                    if (!LevelManager.I.currentLevel.hideUI) UI.I.ingame.Toggle(true);
                    LevelManager.I.worldOffsetX = 0;
                    LevelManager.I.worldOffsetY = 0;
                    LevelManager.I.ReloadLevel();
                }
            }
        }
    }
}
