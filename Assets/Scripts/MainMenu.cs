using UnityEngine;
using UnityEngine.UI;
using static TransitionManager.Transitions;

public class MainMenu : MonoBehaviour
{
    [HideInInspector] public static MainMenu I;
    public Text version;
    public Text debug;

    private void Start()
    {
        I = this; // No persistence!
        
        // Version text
        version.text = $"v{Application.version}";
    }

    // Play button event
    public void Play()
    {
        if (GameManager.save.game.doPrologue) TransitionManager.Instance.TransitionIn<string>(Reveal, ActionPrologue);
        else UI.Instance.ChangeScene("Hub");
    }

    // Actions //
    private void ActionPrologue(string _)
    {
        // Loads the level
        GameManager.save.game.doPrologue = false;
        LevelManager.Instance.LoadLevel("PROLOGUE/BEGIN");
        LevelManager.Instance.RefreshGameVars();
        UI.Instance.ChangeScene("Game", false);
        
        DialogManager.Instance.StartDialog(Resources.Load<DialogScriptable>("Dialog/Prologue/Start"), "Dialog/Prologue/Start");
    }
}
