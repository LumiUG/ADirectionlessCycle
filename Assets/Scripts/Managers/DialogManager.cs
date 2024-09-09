using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static GameTile;

public class DialogManager : MonoBehaviour
{
    [HideInInspector] public static DialogManager Instance;

    public DialogScriptable loadedDial;
    public string[] dialog;
    public DialogEvent[] events = { new() };
    public string npcName = "???";
    public float textSpeed = 0.05f;
    public bool canBeSkipped = true;
    public bool shouldExhaust = true;
    public bool inDialog = false;

    private bool ignoreNewChatSource;
    private bool hasDialogStarted;
    private bool canInteract;
    private int dialogIndex;
    private string currentDialogPath;

    public void Awake()
    {
        // Singleton (DialogManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        ResetPrivatesToDefaultState();
        inDialog = false;
    }

    // Starts the dialog with the NPC
    public void StartDialog(DialogScriptable chat, string dialogPath)
    {
        if (!canInteract || !chat) return;
        currentDialogPath = dialogPath;
        inDialog = true;

        // Check if we should use the exhausted dialog instead
        if (GameManager.save.game.exhaustedDialog.Contains(dialogPath)) chat = chat.exhaustDialog;

        // Should we change/load the new scriptable?
        if (!loadedDial || chat != loadedDial && !ignoreNewChatSource) DelegateScriptable(chat);

        // Not the first time?
        if (hasDialogStarted) { ProceedChat(); return; }

        // Toggles the dialog box on
        UI.Instance.dialog.Toggle(true);
    
        // Resets the dialog box
        UI.Instance.dialog.name.text = npcName;
        UI.Instance.dialog.text.text = string.Empty;
        hasDialogStarted = true;

        // Reads a line
        NextLine();
    }

    private void ProceedChat(bool forceNext = false)
    {
        // Go to the next line of dialog
        if (UI.Instance.dialog.text.text == dialog[dialogIndex] || forceNext) { dialogIndex++; NextLine(); return; }

        // Stop showing text and show it all instantly
        if (!canBeSkipped) return;
        ShowEntireLine();
    }

    // Changes dialog line to a next one or ends the conversation
    private void NextLine()
    {
        // Goes to the next line of dialog
        if (dialogIndex < dialog.Length)
        {
            // Executes a dialog event
            foreach (DialogEvent ev in events)
            {
                if (ev.executeAtIndex == dialogIndex)
                {
                    ev.textSpeedEvent.Run();
                    ev.setTileEvent.Run();
                }
            }
            
            // Reads the line
            StartCoroutine(ReadLine());
            return;
        }

        // Ends dialog
        UI.Instance.dialog.Toggle(false);
        ResetPrivatesToDefaultState();
        inDialog = false;

        // Has the chat ended? Exhaust the chat
        if (!shouldExhaust || LevelManager.Instance.currentLevelID == LevelManager.Instance.levelEditorName) return;
        if (!GameManager.save.game.exhaustedDialog.Contains(currentDialogPath)) GameManager.save.game.exhaustedDialog.Add(currentDialogPath);
    }

    // Reads a line of text
    public IEnumerator ReadLine()
    {
        // Resets dialogbox text
        UI.Instance.dialog.SetText(string.Empty);

        // Draws every character on the dialogbox
        foreach (char c in dialog[dialogIndex])
        {
            UI.Instance.dialog.SetText(c.ToString(), true);
            if (loadedDial.sfx != null) AudioManager.Instance.PlaySFX(loadedDial.sfx, 0.60f);
            if (c.ToString() != "." && c.ToString() != ",") yield return new WaitForSecondsRealtime(textSpeed);
            else yield return new WaitForSecondsRealtime(textSpeed + 0.1f);
        }
    }

    // Change dialog scriptable (delegate or fucking whatever its called idk man)
    public void DelegateScriptable(DialogScriptable newDialog, bool doDefaults = false) {
        dialog = newDialog.dialog;
        events = newDialog.events;
        npcName = newDialog.npcName;
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
        UI.Instance.dialog.SetText(dialog[dialogIndex]);
        StopAllCoroutines();
    }


    // Dialog events
    [Serializable] public class DialogEvent {
        public EventTextSpeed textSpeedEvent;
        public EventSetTile setTileEvent;
        public int executeAtIndex = 0;

        // Required to have Run() or something idk
        [Serializable] public abstract class EventAction {
            public bool enabled = false;
            public abstract void Run();
        }

        // Change text speed
        [Serializable] public class EventTextSpeed : EventAction
        {
            public float textSpeed;
            public override void Run()
            {
                if (!enabled) return;
                Instance.textSpeed = textSpeed;
            }
        }

        // Set a tile
        [Serializable] public class EventSetTile : EventAction
        {
            public ObjectTypes setAs = new();
            public Vector3Int position = new();
            public bool delete = false;
            public override void Run()
            {
                if (!enabled) return;
                if (!delete) LevelManager.Instance.PlaceTile(LevelManager.Instance.CreateTile(setAs.ToString(), new(), position));
                else {
                    GameTile tile;
                    tile = setAs switch
                    {
                        var t when LevelManager.Instance.typesSolidsList.Contains(t) => LevelManager.Instance.tilemapCollideable.GetTile<GameTile>(position),
                        var t when LevelManager.Instance.typesAreas.Contains(t) => LevelManager.Instance.tilemapWinAreas.GetTile<GameTile>(position),
                        var t when LevelManager.Instance.typesHazardsList.Contains(t) => LevelManager.Instance.tilemapHazards.GetTile<GameTile>(position),
                        var t when LevelManager.Instance.typesEffectsList.Contains(t) => LevelManager.Instance.tilemapEffects.GetTile<GameTile>(position),
                        var t when LevelManager.Instance.typesCustomsList.Contains(t) => LevelManager.Instance.tilemapCustoms.GetTile<GameTile>(position),
                        _ => LevelManager.Instance.tilemapObjects.GetTile<GameTile>(position),
                    };
                    if (tile) LevelManager.Instance.RemoveTile(tile);
                }
            }
        }
    }
}
