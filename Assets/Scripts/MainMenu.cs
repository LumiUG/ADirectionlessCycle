using UnityEngine;
using UnityEngine.UI;
using static TransitionManager.Transitions;

public class MainMenu : MonoBehaviour
{
    public Text version;

    private void Start()
    {
        // Version text
        version.text = $"v{Application.version}";
    }

    // 
    public void Play()
    {
        // if (GameManager.save.game.doPrologue) TransitionManager.Instance.TransitionIn<string>(Reveal, ActionPrologue);
        UI.Instance.ChangeScene("Hub");
    }

    // Actions //
    private void ActionPrologue(string _)
    {
        // Loads the level
        GameManager.save.game.doPrologue = false;
        LevelManager.Instance.LoadLevel("PROLOGUE/Landing");
        LevelManager.Instance.RefreshGameVars();
        UI.Instance.ChangeScene("Game", false);
        
        DialogManager.Instance.StartDialog(Resources.Load<DialogScriptable>("Dialog/Prologue/Start"), "Dialog/Prologue/Start");
    }
}
