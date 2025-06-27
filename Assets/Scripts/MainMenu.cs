using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [HideInInspector] public static MainMenu I;
    [Header("Badges")]
    public RectTransform debugIcon;
    public RectTransform mimicIcon;
    public RectTransform masteryIcon;
    public RectTransform allMainIcon;
    public RectTransform allRemixIcon;
    public RectTransform allOutboundIcon;
    public RectTransform trialIcon;
    public GameObject badgeHolder;
    
    [Header("Misc")]
    public GameObject scorchingStupid;
    public GameObject postgameChecker;
    public Button playBtn;
    public Text version;
    public Text debug;

    [Header("Popup")]
    public Button popupBtn;
    public Text popupText;

    [Header("Trials")]
    public GameObject trialInfo;

    private void Start()
    {
        I = this; // No persistence!

        if (GameManager.save.game.hasCompletedGame) postgameChecker.SetActive(true);

        // Setup font changes
        if (GameManager.save.preferences.accessibleFont)
        {
            popupText.font = GameManager.I.acessibilityFont;
            UI.I.dialog.text.font = GameManager.I.acessibilityFont;
            UI.I.popup.popupText.font = GameManager.I.acessibilityFont;
        } else {
            UI.I.dialog.text.font = GameManager.I.originalFont;
            UI.I.popup.popupText.font = GameManager.I.originalFont;
        }

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
        if (TransitionManager.I.inTransition) return;
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
        trialIcon.gameObject.SetActive(GameManager.I.isDoingTrial);

        // Apply offset
        int offset = 0;
        if (debugIcon.gameObject.activeSelf) { debugIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (mimicIcon.gameObject.activeSelf) { mimicIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (masteryIcon.gameObject.activeSelf) { masteryIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (allMainIcon.gameObject.activeSelf) { allMainIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (allRemixIcon.gameObject.activeSelf) { allRemixIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (allOutboundIcon.gameObject.activeSelf) { allOutboundIcon.anchoredPosition = new(offset, 0); offset -= 125; }
        if (trialIcon.gameObject.activeSelf) { trialIcon.anchoredPosition = new(offset, 0); }

        // Scorching thing
        scorchingStupid.SetActive(GameManager.save.game.exhaustedDialog.Find(dialog => dialog == "EXHAUST-EXHAUST-Dialog/Scorch/Hi") != null);
    }

    public void GoCustoms()
    {
        if (TransitionManager.I.inTransition) return;
        
        if (!GameManager.save.game.hasCompletedGame && !GameManager.save.game.seenSpoilerWarning)
        {
            GameManager.save.game.seenSpoilerWarning = true;
            ShowPopup("The level editor is recommended for players who've completed the game, though, if you'd like to use it, be mindful of spoilers.\nThis popup won't appear again!");
            return;
        }
        UI.I.ChangeScene("Custom Levels");
    }

    public void HidePopup()
    {
        UI.I.selectors.ChangeSelected(playBtn.gameObject);
        popupText.transform.parent.gameObject.SetActive(false);
    }
    internal void ShowPopup(string text)
    {
        popupText.transform.parent.gameObject.SetActive(true);
        UI.I.selectors.ChangeSelected(popupBtn.gameObject);
        popupText.text = text;
    }

    // MEOW
    public void Meow()
    {
 
    }

    public void UnMeow()
    {

    }

    private List<int> LoadTrial(bool ignore = false)
    {
        return null;
    }

    public void TrialType(bool type)
    {

    }
}
