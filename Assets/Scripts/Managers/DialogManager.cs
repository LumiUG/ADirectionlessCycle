using System;
using System.Collections;
using UnityEngine;

public class DialogManager : MonoBehaviour
{
    [HideInInspector] public static DialogManager Instance;

    public DialogScriptable loadedDial;
    public string[] dialog;
    public DialogEvent[] events = { new() };
    public string npcName = "Stranger";
    public float textSpeed = 0.05f;
    public bool canBeSkipped = true;
    public bool inDialog = false;

    private bool ignoreNewChatSource;
    private bool hasDialogStarted;
    private bool canInteract;
    private int dialogIndex;

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
    public void StartDialog(DialogScriptable chat)
    {
        if (!canInteract || !chat) return;
        inDialog = true;

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
            foreach (DialogEvent ev in events) { if (ev.executeAtIndex == dialogIndex) ev.textSpeedEvent.Run(); }
            
            // Reads the line
            StartCoroutine(ReadLine());
            return;
        }

        // Ends dialog
        UI.Instance.dialog.Toggle(false);
        ResetPrivatesToDefaultState();
        inDialog = false;
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
    }
}
