using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TransitionManager.Transitions;
using static GameTile;

public class UI : MonoBehaviour
{
    [HideInInspector] public static UI I;
    [SerializeField] public GlobalUI global;
    [SerializeField] public LevelEditorUI editor;
    [HideInInspector] public PreloadUI preload; 
    [SerializeField] public PauseUI pause;
    [SerializeField] public IngameUI ingame;
    [SerializeField] public ConfirmRestartUI restart;
    [SerializeField] public PopupUI popup;
    [SerializeField] public DialogUI dialog;
    [HideInInspector] public Selectors selectors;

    internal Animator effects;
    private Color invisibleColor = new(1, 1, 1, 0);

    private void Awake()
    {
        // Singleton (UI has persistence)
        if (!I) { I = this; }
        else { Destroy(transform.parent.gameObject); return; }
        DontDestroyOnLoad(transform.parent.gameObject);

        effects = GameObject.Find("Ingame Effects").GetComponent<Animator>();
        DontDestroyOnLoad(effects.gameObject);

        // Global UI
        global = new() { self = gameObject };
        global.debugger = global.self.transform.Find("Debugger").GetComponent<Text>();
        global.debugger.CrossFadeAlpha(0, 0, true);
        global.camera = Camera.main;

        // Preload UI
        preload = new() { self = transform.parent.Find("Intermissions").Find("Level Load").gameObject };
        preload.animator = preload.self.transform.parent.GetComponent<Animator>();
        preload.levelName = preload.self.transform.Find("Level Name").gameObject.GetComponent<Text>();
        preload.stars = preload.self.transform.Find("Difficulty Stars").gameObject;
        preload.starFilledGraphic = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Filled");
        preload.starHollowGraphic = Resources.Load<Sprite>("Sprites/UI/Stars/Star_Hollow");
        preload.time = preload.self.transform.Find("Best Time").gameObject.GetComponent<Text>();
        preload.moves = preload.self.transform.Find("Best Moves").gameObject.GetComponent<Text>();

        // Change from preload scene?
        SceneManager.LoadScene("Main Menu");
    }

    // Change scenes
    public void ChangeScene(string sceneName, bool doTransition = true)
    {
        // Move the UI selectors to its default place
        if (sceneName != "Game") selectors.SetEffect(0);
        if (selectors)
        {
            if (!selectors.left || !selectors.right) return;
            
            selectors.leftImage.color = invisibleColor;
            selectors.rightImage.color = invisibleColor;

            selectors.left.SetParent(selectors.gameObject.transform);
            selectors.right.SetParent(selectors.gameObject.transform);
            selectors.left.anchoredPosition = Vector2.zero;
            selectors.right.anchoredPosition = Vector2.zero;
        }

        // Change scene after transition
        if (doTransition) TransitionManager.I.TransitionIn(Reveal, Actions.ChangeScene, sceneName);
        else SceneManager.LoadScene(sceneName);

        // Anything music related
        if (!AudioManager.I) return;
        switch (sceneName)
        {
            case "Main Menu":
            case "Settings":
            case "Hub":
            case "Bonus":
            case "Credits":
            case "Custom Levels":
                AudioManager.I.PlayBGM(AudioManager.titleBGM);
                break;
            case "Level Editor":
                AudioManager.I.PlayBGM(AudioManager.editorBGM);
                break;
            case "Game": // Do nothing, handle internally with LevelManager.
                break;
            default:
                Debug.LogWarning("No audio scene found");
                break;
        }
    }

    // Exit application
    public void ExitApplication() { Application.Quit(); }

    // Goes to main menu (scary)
    public void GoMainMenu()
    {
        if (SceneManager.GetActiveScene().name == "Level Editor") LevelManager.I.SaveLevel("Editor Mode!", LevelManager.I.levelEditorName);
        TransitionManager.I.TransitionIn<string>(Reveal, Actions.GoMainMenu);
        GameManager.I.SetPresence("steam_display", "#Menuing");
        GameManager.I.UpdateActivity("On the main menu.");
    }

    // Goes to hub (scarier)
    public void GoHub()
    {
        TransitionManager.I.TransitionIn<string>(Swipe, Actions.ReturnHub);
        GameManager.I.SetPresence("steam_display", "#Menuing");
        GameManager.I.UpdateActivity("On the main menu.");
    }

    // Go from a level to the editor
    public void GoLevelEditor()
    {
        // if (!GameManager.I.IsDebug() && !GameManager.save.game.hasCompletedGame) { global.SendMessage("Complete the game first!"); return; }

        // Transition in
        TransitionManager.I.TransitionIn<string>(Reveal, Actions.GoLevelEditor);
    }

    // Pause/Unpause game
    public void PauseUnpauseGame(bool status)
    {
        GameManager.I.PauseResumeGame(status);
    }

    // Clears the UI (disables everything)
    public void ClearUI()
    {
        ingame.Toggle(false);
        editor.Toggle(false);
        pause.Toggle(false);
        // win.Toggle(false);
    }

    // Import level
    public void LevelEditorImportLevel(string levelName)
    {
        if (!GameManager.I.IsEditor()) return;
        LevelManager.I.LoadLevel(levelName, true, false);
    }

    // Export level
    public void LevelEditorExportLevel()
    {
        if (!GameManager.I.IsEditor()) return;

        // Export level
        LevelManager.I.SaveLevel(GameManager.I.currentEditorLevelName, GameManager.I.currentEditorLevelID, true, GameManager.I.SaveLevelPreview());
    }

    // Playtest level
    public void LevelEditorPlaytest()
    {
        if (!GameManager.I.IsEditor()) return;

        LevelManager.I.SaveLevel("Editor Mode!", LevelManager.I.levelEditorName);
        LevelManager.I.currentLevel = LevelManager.I.GetLevel(LevelManager.I.levelEditorName, true);
        LevelManager.I.currentLevelID = LevelManager.I.levelEditorName;
        GameManager.I.isEditing = true;
        LevelManager.I.worldOffsetX = 0;
        LevelManager.I.worldOffsetY = 0;
        LevelManager.I.MoveTilemaps(LevelManager.I.originalPosition, true);
        editor.Toggle(false);
        ingame.Toggle(true);
        
        LevelManager.I.LoadLevel(LevelManager.I.levelEditorName, true);
        ChangeScene("Game");
    }

    // Sets the next level (sanitize input?)
    public void LevelEditorSetNextLevel(string value)
    {
        if (!GameManager.I.IsEditor()) return;
        LevelManager.I.currentLevel.nextLevel = value;
    }

    // Sets the REMIX level (sanitize input?)
    public void LevelEditorSetRemixLevel(string value)
    {
        if (!GameManager.I.IsEditor()) return;
        LevelManager.I.currentLevel.remixLevel = value;
    }

    // Set freeroam value
    public void LevelEditorSetFreeroam(bool value)
    {
        if (!GameManager.I.IsEditor()) return;
        LevelManager.I.currentLevel.freeroam = value;
    }

    // Goto next level
    public void GoNextLevel()
    {
        // Player is playtesting
        if (GameManager.I.isEditing)
        {
            GoLevelEditor();
            return;   
        }

        // There's no next level.
        if (string.IsNullOrEmpty(LevelManager.I.currentLevel.nextLevel))
        {
            GoHub();
            return;
        }
        
        // Next level.
        TransitionManager.I.TransitionIn<string>(Triangle, Actions.GoNextLevel);
        GameManager.I.isEditing = false;
    }

    // Restart current level
    public void RestartLevel(bool restartScreen = false)
    {
        if (LevelManager.I.currentLevel == null) return;
        if (restartScreen) CloseConfirmRestart();
        TransitionManager.I.TransitionIn<string>(Swipe, Actions.UIRestartLevel);
    }

    // Remove restart screen
    public void CloseConfirmRestart() { restart.Toggle(false); }

    // Remove popup
    public void ClosePopup() { selectors.ChangeSelected(pause.backToMenu, true); popup.Toggle(false); }

    // UI confirm sound
    public void ConfirmSound()
    {
        AudioManager.I.PlaySFX(AudioManager.select, 0.35f, true);
    }

    // Goes to the current level's
    public void CurrentLevelHint()
    {
        if (LevelManager.I.currentLevel == null) return;

        // Prepare the hint level's ID
        string[] split = LevelManager.I.currentLevelID.Split("/");
        string hintLevelID;

        // Check for custom levels
        if (split[0] != LevelManager.I.currentLevelID)
        {
            if (split[0].Contains("REMIX") || split[0].Contains("FRAGMENTS")) hintLevelID = $"HINTS/{split[1]}H";
            else if (split[0].Contains("HINTS")) hintLevelID = LevelManager.I.currentLevel.nextLevel;
            else hintLevelID = $"HINTS/W{split[1]}H";

            // Custom handlings
            if (GameManager.save.game.hasCompletedGame && !split[0].Contains("VOID") && !split[0].Contains("CODE") && LevelManager.I.GetLevel(hintLevelID, false, true) == null) { popup.SetPopup("..."); return; }
            switch (split[1])
            {
                case "Orb One":
                    {
                        if (GameManager.save.game.exhaustedDialog.Find(dialog => dialog == "EXHAUST-Dialog/3-12/Light") == null) popup.SetPopup("[Proceed further to reveal this hint]");
                        else popup.SetPopup("Fine... It's \"Seek the path of light.\"");
                        return;
                    }
                case "Orb Two": { popup.SetPopup("...We've got no idea how to get there."); return; }
                case "Orb Three": { popup.SetPopup("Pretty straightforward!"); return; }
                case "Fragment TwoH": { popup.SetPopup("I won't let you go deeper."); return; }
                case "Fragment Three": { popup.SetPopup("I'd disallow it, but there's really nothing, sorry!"); return; }
                case "3-1": { popup.SetPopup("You aren't getting in this easily."); return; }
                case "3-10": { popup.SetPopup("No, no, and no. You're not getting a hint."); return; }
                case "Industrial": { popup.SetPopup("I'm sure you can figure out this one yourself!"); return; }
                case "Meem": { popup.SetPopup("The Gravix dog? ...Thing? It was here the entire time???"); return; }
                case "Upgrade": { popup.SetPopup("I'm... Definitely not serving you a hint here. Please stop."); return; }
                case "Tutorial": { popup.SetPopup("Player... They're already giving you a tutorial."); return; }
                case "END": { popup.SetPopup("The core's entrance. The point of no return."); return; }
                case "Entry": { popup.SetPopup("%begin% CO*MU%%CAT$0N %end%"); return; }
                case "Corridor": { popup.SetPopup("gin% ENTERING *##$$! AREA"); return; }
                case "Right": { popup.SetPopup("%end%end%end%end%endddddd"); return; }
                case "Down": { popup.SetPopup("WRITE LATER"); return; }
                case "Left": { popup.SetPopup("WRITE LATER"); return; }
                case "Up": { popup.SetPopup("WRITE LATER"); return; }
                case "Outro": { popup.SetPopup("[Nothing]"); return; }
                case "Despair": { popup.SetPopup("In an invisible maze, no one can hear you scream."); return; }
                case "Quiz": { popup.SetPopup("Missed opportunity to make an answer rely on the level hint. Shame on me."); return; }
                case "Developer": { popup.SetPopup("Here for a peek? Carry on, carry on."); return; }
                default: // Custom cases
                    if (LevelManager.I.currentLevelID == "VOID/Loop") { popup.SetPopup("internal abstract GameTile Loop();"); return; }
                    break;
            }
        } else hintLevelID = null;

        // Get the level and load it accordingly
        if (LevelManager.I.GetLevel(hintLevelID, false, true) == null) { popup.SetPopup("This level has no hints available."); return; }
        TransitionManager.I.TransitionIn(Triangle, Actions.GoHintLevel, hintLevelID);
    }

    // Object classes //
    public abstract class UIObject
    {
        public GameObject self;

        public virtual void Toggle(bool status) { if (self) self.SetActive(status); }
    }

    public class GlobalUI : UIObject
    {
        public Camera camera;
        public Text debugger;

        // Sends a message log to the editor UI
        public void SendMessage(string message, float duration = 1.0f)
        {
            debugger.text = message;
            debugger.CrossFadeAlpha(1, 0, true);
            debugger.CrossFadeAlpha(0, duration, true);
        }
    }

    [Serializable]
    public class LevelEditorUI : UIObject
    {
        public GameObject import;
        public GameObject playtest;
        public InputField nextLevelField;
        public InputField remixLevelField;
        public Toggle freeroamToggle;
    }

    public class PreloadUI : UIObject
    {
        public Animator animator;
        public Text levelName;
        public GameObject stars;
        public Sprite starFilledGraphic;
        public Sprite starHollowGraphic;
        public Text time;
        public Text moves;

        public void SetStars(int amount = 1)
        {
            if (amount < 1) amount = 1;
            else if (amount > 9) amount = 9;
            
            // Get list of stars
            List<Image> starList = new();
            foreach (Transform child in stars.transform.Find("Stars")) { starList.Add(child.GetComponent<Image>()); }

            // Enable stars
            starList.ForEach(s => s.sprite = starHollowGraphic);
            foreach (var star in starList)
            {
                if (amount <= 0) break;

                star.sprite = starFilledGraphic;
                amount--;
            }
        }
        public void SetLevelName(string name) { levelName.text = name; }

        public void SetBestTime(float newTime = -1)
        {
            if (newTime == -1 ) time.text = "Best time: ???";
            else time.text = $"Best time: {Math.Round(newTime, 2)}s";
        }

        public void SetBestMoves(int newMoves = -1)
        {
            if (newMoves == -1) moves.text = "Best moves: ???";
            else moves.text = $"Best moves: {newMoves}";
        }

        internal void PreparePreloadScreen(Serializables.GameData.Level save)
        {
            SetLevelName(LevelManager.I.currentLevel.levelName);
            SetStars(LevelManager.I.currentLevel.difficulty);
            if (save != null) { SetBestTime(save.stats.bestTime); SetBestMoves(save.stats.totalMoves); }
            else { SetBestTime(-1); SetBestMoves(-1); }
            Toggle(true);
            animator.Play("Load In");
        }
    }

    [Serializable]
    public class WinUI : UIObject
    {
        public Animator animator;
        public Text completeText;
        public Transform stats;
        public Text time;
        public Text moves;

        public void SetTotalTime(float newTime) { time.text = $"Total time: {Math.Round(newTime, 2)}s"; }
        public void SetTotalMoves(int newMoves) { moves.text = $"Total moves: {newMoves}"; }
    }

    [Serializable]
    public class PauseUI : UIObject
    {
        public GameObject resumeButton;
        public GameObject editorButton;
        public GameObject backToMenu;
        public Transform levelInfo;
        public Text title;
        public Text levelBestTime;
        public Text levelBestMoves;

        public void ToggleEditButton(bool toggle) { editorButton.SetActive(toggle); }
        public void SetBestTime(float newTime) { levelBestTime.text = $"Best time: {Math.Round(newTime, 2)}s"; }
        public void SetBestMoves(int newMoves) { levelBestMoves.text = $"Best moves: {newMoves}"; }
    }

    [Serializable]
    public class IngameUI : UIObject
    {
        public RectTransform rArea;
        public RectTransform rTimer;
        public RectTransform rMoves;
        public Text levelName;
        public Text levelMoves;
        public Text levelTimer;
        public Image areaIcon;
        public Image cycleIcon;
        public Text areaCount;

        public void SetAreaIcon(int icon)
        {
            switch (icon)
            {
                case 1:
                    areaIcon.sprite = LevelManager.I.areaTile.tileSprite;
                    break;
                case 2:
                    areaIcon.sprite = LevelManager.I.inverseAreaTile.tileSprite;
                    break;
                case 3:
                    areaIcon.sprite = LevelManager.I.outboundAreaTile.tileSprite;
                    break;
            }
        }
        public void SetLevelMoves(int newMoves) { levelMoves.text = $"{newMoves}"; }
        public void SetLevelTimer(float newTime) { levelTimer.text = $"{Math.Round(newTime, 2)}s"; }
        public void SetAreaCount(int current, int max, int type)
        {
            // Area overlapped SFX
            int.TryParse(areaCount.text.Split("/")[0], out int areaNum);
            if (AudioManager.I && current > areaNum)
            {
                switch (type)
                {
                    case 1:
                        AudioManager.I.PlaySFX(AudioManager.areaOverlap, 0.35f);
                        break;
                    case 2:
                        AudioManager.I.PlaySFX(AudioManager.inverseOverlap, 0.35f);
                        break;
                    case 3:
                        AudioManager.I.PlaySFX(AudioManager.outboundOverlap, 0.35f);
                        break;
                }
            }

            // Update UI
            areaCount.text = $"{current}/{max}";
            SetAreaIcon(type);
        }
        public void SetCycleIcon(ObjectTypes tile)
        {
            Sprite spr;
            switch (tile)
            {
                case ObjectTypes.Box:
                    spr = LevelManager.I.boxTile.tileSprite;
                    break;
                case ObjectTypes.Circle:
                    spr = LevelManager.I.circleTile.tileSprite;
                    break;
                case ObjectTypes.Hexagon:
                    spr = LevelManager.I.hexagonTile.tileSprite;
                    break;
                case ObjectTypes.Mimic:
                    spr = LevelManager.I.mimicTile.tileSprite;
                    break;
                default:
                    return;
            }
            cycleIcon.sprite = spr;
        }
    }

    [Serializable]
    public class ConfirmRestartUI : UIObject
    {
        public Button restartButton;
        public override void Toggle(bool toggle)
        {
            self.SetActive(toggle);
            if (toggle) I.selectors.ChangeSelected(restartButton.gameObject, true);
        }
    }

    [Serializable]
    public class PopupUI : UIObject
    {
        public GameObject popupBtn;
        public Text popupText;
        public void SetPopup(string content)
        {
            I.selectors.ChangeSelected(popupBtn, true);
            popupText.text = content;
            Toggle(true);
        }
    }

    [Serializable]
    public class DialogUI : UIObject
    {
        public Text text;
        public void SetText(string newText, bool additive = false)
        {
            if (additive) text.text += $"{newText}";
            else text.text = $"{newText}";
        }
    }
}
