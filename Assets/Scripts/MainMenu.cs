using UnityEngine;
using UnityEngine.UI;
using static TransitionManager.Transitions;

public class MainMenu : MonoBehaviour
{
    [HideInInspector] public static MainMenu I;
    public RectTransform debugIcon;
    public RectTransform mimicIcon;
    public RectTransform masteryIcon;
    public RectTransform allMainIcon;
    public RectTransform allRemixIcon;
    public RectTransform allOutboundIcon;
    public GameObject scorchingStupid;
    public GameObject badgeHolder;
    public Button playBtn;
    public Button popupBtn;
    public Text popupText;
    public Text version;
    public Text debug;

    private void Start()
    {
        I = this; // No persistence!
        
        // Version text
        version.text = $"v{Application.version}";

        // Savefile icons
        SetupBadges();
    }

    // Play button event
    public void Play()
    {
        if (GameManager.save.game.doPrologue) TransitionManager.Instance.TransitionIn<string>(Reveal, ActionPrologue);
        else UI.Instance.ChangeScene("Hub");
    }

    // Shows main menu badges
    internal void SetupBadges()
    {
        // Toggle on/off
        debugIcon.gameObject.SetActive(GameManager.Instance.IsDebug());
        mimicIcon.gameObject.SetActive(GameManager.Instance.editormimic);
        masteryIcon.gameObject.SetActive(GameManager.save.game.hasMasteredGame);
        allMainIcon.gameObject.SetActive(GameManager.save.game.completedAllMainLevels);
        allRemixIcon.gameObject.SetActive(GameManager.save.game.completedAllRemixLevels);
        allOutboundIcon.gameObject.SetActive(GameManager.save.game.completedAllOutboundLevels);

        // Apply offset
        int offset = 0;
        if (debugIcon.gameObject.activeSelf) { debugIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (mimicIcon.gameObject.activeSelf) { mimicIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (masteryIcon.gameObject.activeSelf) { masteryIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (allMainIcon.gameObject.activeSelf) { allMainIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (allRemixIcon.gameObject.activeSelf) { allRemixIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (allOutboundIcon.gameObject.activeSelf) { allOutboundIcon.anchoredPosition = new(offset, 0); }

        // Scorching thing
        scorchingStupid.SetActive(GameManager.save.game.exhaustedDialog.Find(dialog => dialog == "EXHAUST-EXHAUST-Dialog/Scorch/Hi") != null);
    }

    public void HidePopup()
    {
        UI.Instance.selectors.ChangeSelected(playBtn.gameObject);
        popupText.transform.parent.gameObject.SetActive(false);
    }

    public void GoCustoms()
    {
        if (!GameManager.save.game.hasCompletedGame && !GameManager.save.game.seenSpoilerWarning)
        {
            GameManager.save.game.seenSpoilerWarning = true;
            ShowPopup("The level editor is recommended for players who've completed the game, though, if you'd like to use it, be mindful of spoilers.\nThis popup won't appear again!");
            return;
        }
        UI.Instance.ChangeScene("Custom Levels");
    }

    internal void ShowPopup(string text)
    {
        popupText.transform.parent.gameObject.SetActive(true);
        UI.Instance.selectors.ChangeSelected(popupBtn.gameObject);
        popupText.text = text;
    }

    // Actions //
    private void ActionPrologue(string _)
    {
        // Loads the level
        GameManager.save.game.doPrologue = false;
        LevelManager.Instance.LoadLevel("PROLOGUE/BEGIN");
        LevelManager.Instance.RefreshGameVars();
        UI.Instance.ChangeScene("Game", false);
        
        // DialogManager.Instance.StartDialog(Resources.Load<DialogScriptable>("Dialog/Prologue/Start"), "Dialog/Prologue/Start");
    }
}
