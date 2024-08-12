using UnityEngine;

[CreateAssetMenu(fileName = "Default Dialog", menuName = "Dialog Scriptable")]
public class DialogScriptable : ScriptableObject
{
    // Scriptable settings
    public string[] dialog = { "Hello, world.", "Programmed to work and not to feel.", "Not even sure that this is real.", "Hello, world." };
    public DialogManager.DialogEvent[] events = { new() };
    public string npcName = "?????";
    public float textSpeed = 0.05f;
    public AudioClip sfx = null;
    public bool canBeSkipped = true;
}
