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
        if (trialInfo.activeSelf || popupBtn.transform.parent.gameObject.activeSelf) return;

        mewCount++;
        if (mewCount < 3) { AudioManager.I.PlaySFX(AudioManager.meow, 0.7f, true); return; }
        AudioManager.I.PlaySFX(AudioManager.meow, 0.8f, true, 0.8f);

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

    private void LoadTrial()
    {
        if (trialVanilla) trialEffect.shadowColor = GameManager.I.boxColor;
        else trialEffect.shadowColor = GameManager.I.outboundColor;

        TrialScriptable[][] ts = { GameManager.I.trialsAreaOne.ToArray(), GameManager.I.trialsAreaTwo.ToArray(), GameManager.I.trialsAreaThree.ToArray(), GameManager.I.trialsRemix.ToArray() };
        Text[] fields = { trialCountOne, trialCountTwo, trialCountThree, trialCountRemix };
        Image[] amounts = { trialFillOne, trialFillTwo, trialFillThree, trialFillRemix };

        for (int i = 0; i < fields.Count(); i++)
        {
            int totalCount;
            if (trialVanilla) totalCount = ts[i].Count(trial => { return trial.vanillaMoves != -1; });
            else totalCount = ts[i].Count(trial => { return trial.cycleMoves != -1; });

            int count = ts[i].Count(trial =>
            {
                var level = GameManager.save.game.levels.Find(l => l.levelID == trial.levelID);
                if (level == null) return false;
                if (trialVanilla && level.stats.totalMovesNormal > trial.vanillaMoves) return false;
                if (!trialVanilla && level.stats.totalMovesCycle > trial.cycleMoves || level.stats.totalMovesCycle == 0) return false;
                return true;
            });

            fields[i].text = $"{count} / {totalCount}";
            amounts[i].fillAmount = (float)count / totalCount;
        }
    }

    public void TrialType(bool type)
    {
        trialVanilla = type;
        LoadTrial();
    }
}
