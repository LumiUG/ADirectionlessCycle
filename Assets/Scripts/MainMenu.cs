using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    public GameObject postgameChecker;
    public GameObject badgeHolder;
    public Button playBtn;
    public Button popupBtn;
    public Text popupText;
    public Text version;
    public Text debug;

    private void Start()
    {
        I = this; // No persistence!

        if (GameManager.save.game.hasCompletedGame) postgameChecker.SetActive(true);

        // Version text
        version.text = $"v{Application.version}";

        // Savefile icons
        SetupBadges();
    }

    private void Update()
    {
        if (!EventSystem.current) return;
        if (EventSystem.current.currentSelectedGameObject == null) UI.I.selectors.ChangeSelected(playBtn.gameObject, true);
        
        if (EventSystem.current.currentSelectedGameObject.name == "Discord") UI.I.selectors.SetEffect(1);
        else UI.I.selectors.SetEffect(0);
    }

    // Play button event
    public void Play()
    {
        // if (GameManager.save.game.doPrologue) TransitionManager.I.TransitionIn<string>(Reveal, ActionPrologue);
        UI.I.ChangeScene("Hub");
    }

    // Shows main menu badges
    internal void SetupBadges()
    {
        // Toggle on/off
        debugIcon.gameObject.SetActive(GameManager.I.IsDebug());
        mimicIcon.gameObject.SetActive(GameManager.I.editormimic);
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
        UI.I.selectors.ChangeSelected(playBtn.gameObject);
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
        UI.I.ChangeScene("Custom Levels");
    }

    internal void ShowPopup(string text)
    {
        popupText.transform.parent.gameObject.SetActive(true);
        UI.I.selectors.ChangeSelected(popupBtn.gameObject);
        popupText.text = text;
    }

    // Actions //
    private void ActionPrologue(string _)
    {
        // Loads the level
        GameManager.save.game.doPrologue = false;
        LevelManager.I.LoadLevel("PROLOGUE/BEGIN");
        LevelManager.I.RefreshGameVars();
        UI.I.ChangeScene("Game", false);
        
        // DialogManager.I.StartDialog(Resources.Load<DialogScriptable>("Dialog/Prologue/Start"), "Dialog/Prologue/Start");
    }
}
