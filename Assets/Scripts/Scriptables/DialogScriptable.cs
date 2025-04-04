using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Default Dialog", menuName = "Dialog Scriptable")]
[Serializable]
public class DialogScriptable : ScriptableObject
{
    // Scriptable settings
    public string[] dialog = { "Hello, world.", "Programmed to work and not to feel.", "Not even sure that this is real.", "Hello, world." };
    public DialogManager.DialogEvent[] events = { };
    public DialogScriptable exhaustDialog = null;
    public float textSpeed = 0.05f;
    public AudioClip sfx = null;
    public bool shouldExhaust = false;
    public bool canBeSkipped = true;
}
