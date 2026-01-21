using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using ADC.Localization;
using static TransitionManager.Transitions;

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
    public UIEffect trialEffect;
    public Button trialBack;
    public Text trialCountOne;
    public Image trialFillOne;
    public Text trialCountTwo;
    public Image trialFillTwo;
    public Text trialCountThree;
    public Image trialFillThree;
    public Text trialCountRemix;
    public Image trialFillRemix;
    private bool trialVanilla = true;
    private int mewCount = 0;
    private readonly string[] menuSelectorEffect = { "Discord", "Spanish", "English" };
    private readonly int[] trialClearsVanilla = { 12, 11, 9, 16 };
    private readonly int[] trialClearsCycle = { 5, 9, 3, 9 };

    private void Start()
    {
        I = this; // No persistence!

        if (GameManager.save.game.hasCompletedGame) postgameChecker.SetActive(true);

        // Setup settings changes
        LevelManager.I.animatedTilemapScanlines.gameObject.SetActive(GameManager.save.preferences.scanlineAnimation);
        LevelManager.I.staticTilemapScanlines.gameObject.SetActive(!GameManager.save.preferences.scanlineAnimation);
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
        
        if (menuSelectorEffect.Contains(EventSystem.current.currentSelectedGameObject.name)) UI.I.selectors.SetEffect(1);
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
            ShowPopup("CustomLevelsPopup");
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
        popupText.text = Localization.GetStrings("UI", text);
    }

    // MEOW
    public void Meow()
    {
        if (!trialIcon.gameObject.activeSelf) return;
        if (trialInfo.activeSelf || popupBtn.transform.parent.gameObject.activeSelf) return;
        AudioManager.I.PlaySFX(AudioManager.meow, 0.7f, true);

        mewCount++;
        if (mewCount < 3) return;

        // Achievement
        trialVanilla = false; var validationC = LoadTrial(true);
        trialVanilla = true; var validationV = LoadTrial(true);

        bool ach = true;
        for (int i = 0; i < validationV.Count(); i++)
        {
            if (validationV[i] < trialClearsVanilla[i]) { ach = false; break; }
            if (validationC[i] < trialClearsCycle[i]) { ach = false; break; }
        } if (ach) GameManager.I.EditAchivement("ACH_TRIALS");

        // Activation
        trialInfo.SetActive(true);
        UI.I.selectors.ChangeSelected(trialBack.gameObject, true);
        mewCount = 0;
        LoadTrial();
    }

    public void UnMeow()
    {
        UI.I.selectors.ChangeSelected(playBtn.gameObject);
        trialInfo.SetActive(false);
    }

    private List<int> LoadTrial(bool ignore = false)
    {
        TrialScriptable[][] ts = { GameManager.I.trialsAreaOne.ToArray(), GameManager.I.trialsAreaTwo.ToArray(), GameManager.I.trialsAreaThree.ToArray(), GameManager.I.trialsRemix.ToArray() };
        Text[] fields = { trialCountOne, trialCountTwo, trialCountThree, trialCountRemix };
        Image[] amounts = { trialFillOne, trialFillTwo, trialFillThree, trialFillRemix };
        List<int> validation = new();

        if (!ignore)
        {
            if (trialVanilla) trialEffect.shadowColor = GameManager.I.boxColor;
            else trialEffect.shadowColor = GameManager.I.outboundColor;            
        }

        for (int i = 0; i < fields.Count(); i++)
        {
            int totalCount;
            if (trialVanilla) totalCount = ts[i].Count(trial => { return trial.vanillaMoves != -1; });
            else totalCount = ts[i].Count(trial => { return trial.cycleMoves != -1; });

            int count = ts[i].Count(trial =>
            {
                var level = GameManager.save.game.levels.Find(l => l.levelID == trial.levelID);
                if (level == null) return false;
                if (trialVanilla && (level.stats.totalMovesNormal > trial.vanillaMoves || level.stats.totalMovesNormal == 0)) return false;
                if (!trialVanilla && (level.stats.totalMovesCycle > trial.cycleMoves || level.stats.totalMovesCycle == 0)) return false;
                return true;
            });
            validation.Add(count);

            if (ignore) continue;
            fields[i].text = $"{count} / {totalCount}";
            amounts[i].fillAmount = (float)count / totalCount;
        }

        return validation;
    }

    public void TrialType(bool type)
    {
        trialVanilla = type;
        LoadTrial();
    }

    public void SetLocale(int index)
    {
        if (LocalizationSettings.SelectedLocale == Localization.GetLocale(index)) { UI.I.BadSound(); return; }
        
        TransitionManager.I.TransitionIn(Reveal, Actions.SetLocale, $"{index}");
    }
}
