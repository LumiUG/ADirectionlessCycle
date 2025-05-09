using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static TransitionManager.Transitions;
using static GameTile;

public class InputManager : MonoBehaviour
{
    public static InputManager I;
    public Vector3Int latestMovement = Vector3Int.back;

    internal ObjectTypes latestTile = ObjectTypes.Hexagon;
    internal int restartCount = 0;

    private bool isHoldingMovement = false;
    private bool isHoldingUndo = false;
    private Coroutine outCoro = null;
    private Coroutine movementCoro = null;
    private Coroutine undoCoro = null;
    private readonly float repeatMovementCD = 0.14f; // 0.12f default
    private readonly float manualMovementCD = 0.04f; // 0.12f default
    private float currentMovementCD = 0f;

    // Debug //
    private bool canInputCommands = false;
    private string confirmCommand = null;
    private string debugCommand = null;

    void Awake()
    {       
        // Singleton (InputManager has persistence)
        if (!I) { I = this; }
        else { Destroy(gameObject); return; }

        // Defaults
        latestMovement = Vector3Int.back;
    }
    
    void Update()
    {
        // Next level / Load level intermission
        if (TransitionManager.I.inTransition && UI.I.preload.self.activeSelf && Input.anyKeyDown)
        {
            if (outCoro != null) return;
            outCoro = StartCoroutine(OutTransition());
            return;
        }

        // Debug input
        if (!canInputCommands || SceneManager.GetActiveScene().name != "Main Menu") return;

        // Write a command
        if (Input.anyKeyDown)
        {
            string keyboardInput = Input.inputString.Trim().ToLower();
            if (string.IsNullOrEmpty(keyboardInput))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) keyboardInput = "U";
                else if (Input.GetKeyDown(KeyCode.DownArrow)) keyboardInput = "D";
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) keyboardInput = "L";
                else if (Input.GetKeyDown(KeyCode.RightArrow)) keyboardInput = "R";
            }
            debugCommand += keyboardInput;

            if (debugCommand.Length > 10) debugCommand = debugCommand[..10];
            MainMenu.I.debug.CrossFadeAlpha(1f, 0f, true);
            MainMenu.I.debug.text = debugCommand;
        }

        // Null check
        if (debugCommand == null) return;

        // Enable debug command
        if (debugCommand == "adastra")
        {
            if (!GameManager.I.buildDebugMode)
            {
                GameManager.I.buildDebugMode = true;
                MainMenu.I.ShowPopup("Hey! This mode is intended for developers/testers only, if you found this, and want to try it, I am not responsible for your savefile!");
            } else {
                GameManager.I.buildDebugMode = false;
                UI.I.global.SendMessage("Starlight fades...", 3);
            }
            MainMenu.I.SetupBadges();
            debugCommand = null;
        }

        // Secret title command
        if (debugCommand == "UDLRLRUR")
        {
            canInputCommands = false;
            debugCommand = null;
            UI.I.ChangeScene("Bonus");
        }

        // Chess battle advanced
        if (debugCommand == "cba")
        {
            UI.I.global.SendMessage("Chess Battle Advanced", 3);
            GameManager.I.chessbattleadvanced = !GameManager.I.chessbattleadvanced;
            debugCommand = null;
        }

        // Mimic editor unlock
        if (debugCommand == "overflow")
        {
            GameManager.I.editormimic = !GameManager.I.editormimic;
            if (GameManager.I.editormimic) MainMenu.I.ShowPopup("\"Mimic\" enabled for the editor, expect bugs and overflows. You've been warned.");
            MainMenu.I.SetupBadges();
            debugCommand = null;
            return;
        }

        // Delete savedata and generate a new one
        else if (debugCommand == "zero")
        {
            if (DebugConfirm("This will delete all your data!! Are you sure? (Send the command again)")) return;
            GameManager.I.DeleteSave();
            GameManager.I.CreateSave(true);
            UI.I.global.SendMessage("[ Game reset ]", 4);
            MainMenu.I.SetupBadges();
            debugCommand = null;
            return;
        }

        // Delete savedata and generate a new one
        else if (debugCommand == "swap" && GameManager.I.buildDebugMode)
        {
            if (DebugConfirm("This will unlock an endgame mechanic, if you're sure, run this command again.")) return;
            GameManager.save.game.mechanics.hasSwapUpgrade = true;
            UI.I.global.SendMessage("[ New Ability Unlocked ]", 4);
            debugCommand = null;
        }

        // void testing
        else if (debugCommand == "void" && GameManager.I.buildDebugMode)
        {
            canInputCommands = false;
            debugCommand = null;
            Actions.LoadLevel("VOID/END");
            Actions.DiveIn("1");
            return;
        }

        // Livic
        else if (debugCommand == "caos")
        {
            canInputCommands = false;
            debugCommand = null;
            TransitionManager.I.TransitionIn(Reveal, Actions.LoadLevel, "CODE/Caos");
            return;
        }

        // Developer room
        else if (debugCommand == "lumi")
        {
            canInputCommands = false;
            debugCommand = null;
            TransitionManager.I.TransitionIn(Reveal, Actions.LoadLevel, "CODE/Developer");
            return;
        }

        // Custom handlings
        else if (debugCommand == "code")
        {
            MainMenu.I.ShowPopup("...Come on now.");
            debugCommand = null;
            return;
        }
        else if (debugCommand == "help")
        {
            MainMenu.I.ShowPopup("Help? You want help? You'd better check the discord server, then.");
            debugCommand = null;
            return;
        }
        else if (debugCommand == "please")
        {
            MainMenu.I.ShowPopup("That's some potent magic. But really, try a different magic word.");
            debugCommand = null;
            return;
        }
        else if (debugCommand == "gravix")
        {
            MainMenu.I.ShowPopup("Gravix? I don't know what you're talking about.");
            debugCommand = null;
            return;
        }

        if (debugCommand == null)
        {
            MainMenu.I.debug.CrossFadeAlpha(0f, 1.25f, true);
            AudioManager.I.PlaySFX(AudioManager.areaOverlap, 0.35f);
            GameManager.I.EditAchivement("ACH_ENCODED"); // granted by using any command (except "code", "help", "please", "gravix", "zero", "overflow")
        }
    }

    // Returns if you are past the move cooldown timer
    private bool MoveCDCheck(float cooldown)
    {
        return Time.time < currentMovementCD + cooldown;
    }

    // INGAME (LevelManager) //

    // Player movement
    private void OnMove(InputValue ctx)
    {
        if (!IsAllowedToPlay()) return;

        // Input prevention logic
        Vector3Int movCheck = Vector3Int.RoundToInt(ctx.Get<Vector2>());
        if (movCheck == Vector3Int.zero) { isHoldingMovement = false; return; }

        // Better input handler
        Vector3Int movement = movCheck;
        if (movCheck.x != 0 && movCheck.y != 0)
        {
            if (latestMovement.x == 0) movement = new(movCheck.x, 0);
            else if (latestMovement.y == 0) movement = new(0, movCheck.y);
        }

        // Move a single time
        bool doSingle = true;
        if (!GameManager.save.preferences.repeatInput && MoveCDCheck(manualMovementCD)) return;
        if (GameManager.save.preferences.repeatInput && !MoveCDCheck(repeatMovementCD)) doSingle = false;

        if ((doSingle && !isHoldingMovement && GameManager.save.preferences.repeatInput) || doSingle && !GameManager.save.preferences.repeatInput)
        {
            LevelManager.I.AddUndoFrame();
            LevelManager.I.ApplyGravity(movement);
            currentMovementCD = Time.time;
        }

        // Tile vars
        isHoldingMovement = true;
        latestMovement = movement;

        // Repeat your movement
        if (movementCoro != null) StopCoroutine(movementCoro); 
        movementCoro = StartCoroutine(RepeatMovement(movement));
    }

    // Undo move (also editor)
    private void OnUndo(InputValue ctx)
    {
        if (!IsAllowedToPlay() && !GameManager.I.IsEditor()) return;

        bool holding = ctx.Get<float>() == 1f;
        isHoldingUndo = holding;

        if (!holding) return;
        
        // That one void level
        if (LevelManager.I.currentLevelID == "VOID/Loop") { AudioManager.I.PlaySFX(AudioManager.uiDeny, 0.20f); return; }

        // Undo latest move
        if (undoCoro != null) StopCoroutine(undoCoro);
        undoCoro = StartCoroutine(RepeatUndo());
    }

    // Ping all areas
    private void OnShowOverlaps(InputValue ctx)
    {
        // Shows overlaps
        if (!IsAllowedToPlay() && !TransitionManager.I.inTransition) { LevelManager.I.ShowOverlaps(false); return; }
        LevelManager.I.ShowOverlaps(ctx.Get<float>() == 1f);
    }

    // Restart the level
    private void OnRestart()
    {
        if (!IsAllowedToPlay() || LevelManager.I.voidedCutscene) return;

        // Confirm restart screen
        if (GameManager.save.preferences.forceConfirmRestart)
        {
            UI.I.restart.Toggle(true);
            return;
        }

        // Transition in and out while restarting the level
        TransitionManager.I.TransitionIn<string>(Swipe, Actions.Restart);
    }

    // Repeats a movement
    private IEnumerator RepeatMovement(Vector3Int direction, float speed = 0.005f)
    {
        if (!GameManager.save.preferences.repeatInput) yield break;

        while (direction == latestMovement && isHoldingMovement && IsAllowedToPlay())
        {
            if (!MoveCDCheck(repeatMovementCD))
            {
                // Move
                LevelManager.I.AddUndoFrame();
                LevelManager.I.ApplyGravity(direction);
                currentMovementCD = Time.time;
            }
            yield return new WaitForSeconds(speed);
        }
    }

    // Repeats undoing
    private IEnumerator RepeatUndo(float speed = 0.22f)
    {
        int performed = 0;
        while (isHoldingUndo && IsAllowedToPlay() && LevelManager.I.IsUndoQueueValid())
        {
            LevelManager.I.Undo();
            LevelManager.I.RemoveUndoFrame();
            yield return new WaitForSeconds(speed);

            // Speedup undoing gradually
            performed++;
            if (performed >= 5) { performed = 0; speed -= 0.015f; }
            if (speed <= 0.05f) speed = 0.05f; // speed cap
        }

        // Editor override
        if (GameManager.I.IsEditor())
        {
            while (isHoldingUndo && Editor.I.IsUndoQueueValid())
            {
                Editor.I.Undo();
                yield return new WaitForSeconds(speed);

                // Speedup undoing gradually
                performed++;
                if (performed >= 5) { performed = 0; speed -= 0.015f; }
                if (speed <= 0.05f) speed = 0.05f; // speed cap
            }   
        }
    }

    // External (GameManager) //

    // Pause event
    private void OnPause()
    {
        if (GameManager.I.IsBadScene() || LevelManager.I.hasWon || DialogManager.I.inDialog || TransitionManager.I.inTransition || UI.I.restart.self.activeSelf || LevelManager.I.voidedCutscene) return;
        if (!UI.I.pause.self.activeSelf) GameManager.I.PauseResumeGame(true);
        else GameManager.I.PauseResumeGame(false);
    }

    // Level Editor (Editor) //

    // Places a tile
    private void OnEditorClickGrid()
    {
        if (!GameManager.I.IsEditor() || TransitionManager.I.inTransition) return;
        if (Editor.I.popup.activeSelf) return;
        
        // Checks if you are already multi-placing
        if (Editor.I.multiClick != null)
        {
            StopCoroutine(Editor.I.multiClick);
            Editor.I.multiClick = null;
            return;
        }

        // Multi placing tiles !!
        Editor.I.multiClick = StartCoroutine(Editor.I.MultiPlace());
    }

    // Changes a tile's properties
    private void OnEditorRightClickGrid()
    {
        if (!GameManager.I.IsEditor()) return;
        if (Editor.I.popup.activeSelf) { Editor.I.popup.SetActive(false); return; }
        
        // Checks mouse position
        Vector3Int gridPos = Editor.I.GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.I.editor.self.activeSelf) return;

        // Tile validation and selection
        Editor.I.editingTile = LevelManager.I.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!Editor.I.editingTile) Editor.I.editingTile = LevelManager.I.tilemapCustoms.GetTile<CustomTile>(gridPos);
        if (!Editor.I.editingTile) Editor.I.editingTile = LevelManager.I.tilemapEffects.GetTile<EffectTile>(gridPos); // Only arrow tiles
        if (!Editor.I.editingTile) { UI.I.global.SendMessage($"Invalid tile at position \"{gridPos}\""); return; }

        // Set popup position
        Vector3 screenPos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Editor.I.canvas.transform as RectTransform, screenPos, null, out Vector2 anchorPos);
        anchorPos = new Vector2(Math.Clamp(anchorPos.x, -710, 710), Math.Clamp(anchorPos.y, -202, 202));
        Editor.I.popupRect.anchoredPosition = anchorPos;
        
        Editor.I.popup.SetActive(true);
        Editor.I.ignoreUpdateEvent = true;

        // Custom tiles field (missing loading custom text automatically)
        CustomTile custom = LevelManager.I.tilemapCustoms.GetTile<CustomTile>(Editor.I.editingTile.position);
        if (custom != null) {
            Editor.I.customInputField.interactable = true;
            Editor.I.customInputField.text = custom.customText;
        } else {
            Editor.I.customInputField.interactable = false;
            Editor.I.customInputField.text = string.Empty;
        }

        // Basic
        Editor.I.pushableToggle.interactable = Editor.I.editingTile.directions.editorPushable;
        Editor.I.pushableToggle.isOn = Editor.I.editingTile.directions.pushable;
        Editor.I.upToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.downToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.leftToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.rightToggle.interactable = Editor.I.editingTile.directions.editorDirections;
        Editor.I.upToggle.isOn = Editor.I.editingTile.directions.up;
        Editor.I.downToggle.isOn = Editor.I.editingTile.directions.down;
        Editor.I.leftToggle.isOn = Editor.I.editingTile.directions.left;
        Editor.I.rightToggle.isOn = Editor.I.editingTile.directions.right;
        Editor.I.ignoreUpdateEvent = false;
    }

    // Selects a tile on the editor
    private void OnEditorMiddleClick()
    {
        if (!GameManager.I.IsEditor()) return;
        if (Editor.I.popup.activeSelf) { Editor.I.popup.SetActive(false); return; }

        Vector3Int gridPos = Editor.I.GetMousePositionOnGrid();
        if (gridPos == Vector3.back) return;

        GameTile tile = Editor.I.GetEditorTile(gridPos);
        if (tile) Editor.I.SelectListTile(tile, false);
    }

    // Toggles menu
    private void OnEscape()
    {
        if (!UI.I || !EventSystem.current) return;

        // Editor scene
        if (!GameManager.I.IsEditor()) return;
        if (Editor.I.tileList.activeSelf) { Editor.I.ToggleTileMenu(); return; }
        if (Editor.I.popup.activeSelf) { Editor.I.popup.SetActive(false); return; }

        if (!UI.I.editor.self.activeSelf) UI.I.selectors.ChangeSelected(UI.I.editor.playtest, true);
        UI.I.editor.Toggle(!UI.I.editor.self.activeSelf);
    }

    // Save current level manually
    private void OnEditorSave()
    {
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf || Editor.I.popup.activeSelf) return;
        UI.I.LevelEditorExportLevel();
    }

    // Select deleting/placing tiles
    private void OnEditorDelete(InputValue ctx)
    {
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf || Editor.I.popup.activeSelf) return;
        Editor.I.isPlacing = ctx.Get<float>() != 1f;
    }

    // Editor toggle tile menu
    private void OnEditorTileMenu()
    {
        if (!GameManager.I.IsEditor()) return;
        Editor.I.ToggleTileMenu();
    }

    // Editor select tiles 1/2/3/4
    private void OnEditorSelectOne()
    {
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(0);
    }
    private void OnEditorSelectTwo()
    {
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(1);
    }
    private void OnEditorSelectThree()
    {
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(2);
    }
    private void OnEditorSelectFour()
    {
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(3);
    }

    // Moving the level screen up/down/left/right
    private void OnEditorUp() 
    { 
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf || Editor.I.popup.activeSelf || TransitionManager.I.inTransition) return;

        LevelManager.I.MoveTilemaps(new Vector3(0, -8));
        LevelManager.I.worldOffsetY += 8;
    }
    private void OnEditorDown() 
    { 
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf || Editor.I.popup.activeSelf || TransitionManager.I.inTransition) return;

        LevelManager.I.MoveTilemaps(new Vector3(0, 8));
        LevelManager.I.worldOffsetY -= 8;
    }
    private void OnEditorLeft() 
    { 
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf || Editor.I.popup.activeSelf || TransitionManager.I.inTransition) return;

        LevelManager.I.MoveTilemaps(new Vector3(14, 0));
        LevelManager.I.worldOffsetX -= 14;
    }
    private void OnEditorRight() 
    { 
        if (!GameManager.I.IsEditor() || UI.I.editor.self.activeSelf || Editor.I.popup.activeSelf || TransitionManager.I.inTransition) return;

        LevelManager.I.MoveTilemaps(new Vector3(-14, 0));
        LevelManager.I.worldOffsetX += 14;
    }

    // Hub //

    private void OnSwipeLeft()
    {
        if (SceneManager.GetActiveScene().name == "Hub")
        {
            Hub.I.ChangeWorld(-1);
            return;
        }

        if (SceneManager.GetActiveScene().name == "Settings")
        {
            SettingsMenu.I.ToggleMenu(SettingsMenu.I.menuIndex - 1);
        }
    }

    private void OnSwipeRight()
    {
        if (SceneManager.GetActiveScene().name == "Hub")
        {
            Hub.I.ChangeWorld(1);
            return; 
        }

        if (SceneManager.GetActiveScene().name == "Settings")
        {
            SettingsMenu.I.ToggleMenu(SettingsMenu.I.menuIndex + 1);
        }
    }

    // Etc //

    private void OnInteract()
    {
        if (GameManager.I.IsBadScene() || LevelManager.I.isPaused) return;

        // Searches for a first valid NPC
        GameTile tl = null; // (garbage collector tysm)
        GameTile npc = LevelManager.I.GetCustomTiles().Find(
            npc =>
            {
                if (npc.GetTileType() != ObjectTypes.NPC || !LevelManager.I.CheckSceneInbounds(npc.position)) return false;

                GameTile test = null;
                test = LevelManager.I.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(1, 0, 0));
                if (!test) test = LevelManager.I.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(-1, 0, 0));
                if (!test) test = LevelManager.I.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(0, 1, 0));
                if (!test) test = LevelManager.I.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(0, -1, 0));
                if (!test) { test = LevelManager.I.tilemapObjects.GetTile<GameTile>(npc.position); tl = test; } // inner
                if (test) { if (test.directions.GetActiveDirectionCount() > 0) return npc; }
                return false;
            }
        );

        // Gets the NPC and triggers it
        if (npc) { LevelManager.I.tilemapCustoms.GetTile<NPCTile>(npc.position).Effect(tl); }
    }

    // Changes the only tile active's form (might cause issues in the future?)
    private void OnChangeForms()
    {
        if (!IsAllowedToPlay() || !GameManager.save.game.mechanics.hasSwapUpgrade) return;

        // Swap check
        List<GameTile> count = GetPlayableObjects();
        if (count.Count > 1 || count.Count <= 0) { AudioManager.I.PlaySFX(AudioManager.uiDeny, 0.20f); return; }

        // Sfx (change later)
        AudioManager.I.PlaySFX(AudioManager.select, 0.50f);

        // Swap
        LevelManager.I.RemoveTile(count[0]);
        if (count[0].GetTileType() == ObjectTypes.Hexagon) {
            if (latestTile.ToString() == "Hexagon") GameManager.I.EditAchivement("ACH_A_COPY");
            LevelManager.I.PlaceTile(LevelManager.I.CreateTile(latestTile.ToString(), count[0].directions, count[0].position));
            UI.I.ingame.SetCycleIcon(ObjectTypes.Hexagon);
        }
        else {
            LevelManager.I.PlaceTile(LevelManager.I.CreateTile("Hexagon", count[0].directions, count[0].position));
            latestTile = count[0].GetTileType();
            UI.I.ingame.SetCycleIcon(latestTile);
        }
    }

    // Custom level scene scrolling
    private void OnScroll(InputValue ctx)
    {
        if (SceneManager.GetActiveScene().name != "Custom Levels") return;
        float scrollAmount = -(ctx.Get<float>() / 2);

        // Scroll checks
        if (scrollAmount == 0 || CustomLevels.I.popup.activeSelf) return;
        if (CustomLevels.I.holder.anchoredPosition.y + scrollAmount <= -540 && scrollAmount < 0) return;
        if (CustomLevels.I.holder.anchoredPosition.y + scrollAmount >= (CustomLevels.I.rowCount * CustomLevels.I.vertical * -1) - 540 && scrollAmount > 0) return;
        
        // Scrolls by the amount
        CustomLevels.I.holder.anchoredPosition -= Vector2.down * new Vector2(0, scrollAmount);
    }

    // Handles some inputs that should actually prompt an UI exit, but doesnt (thanks, Unity)
    private void OnCustomUIExits()
    {
        if (!EventSystem.current) return;

        switch (SceneManager.GetActiveScene().name)
        {
            case "Custom Levels":
                if (CustomLevels.I.popup.activeSelf) CustomLevels.I.CloseLevelMenu();
                else UI.I.selectors.ChangeSelected(CustomLevels.I.backButton);
                break;
            case "Hub":
                UI.I.selectors.ChangeSelected(Hub.I.backButton.gameObject);
                break;
            case "Settings":
                SettingsMenu.I.ToggleMenu(0);
                UI.I.selectors.ChangeSelected(SettingsMenu.I.menus[0].transform.Find("Back Button").gameObject);
                break;
            case "Credits":
            case "Bonus":
                UI.I.selectors.ChangeSelected(GameObject.Find("Back Button"));
                break;
            case "Game":
                // if (UI.I.restart.self.activeSelf) UI.I.CloseConfirmRestart();
                if (UI.I.popup.self.activeSelf) UI.I.ClosePopup();
                break;
            default:
                break;
        }
    }

    internal bool IsAllowedToPlay() { return !(GameManager.I.IsBadScene() || LevelManager.I.isPaused || LevelManager.I.hasWon || DialogManager.I.inDialog || TransitionManager.I.inTransition || UI.I.restart.self.activeSelf || UI.I.popup.self.activeSelf); }
    internal List<GameTile> GetPlayableObjects() { return LevelManager.I.GetObjectTiles().FindAll(tile => { return tile.directions.GetActiveDirectionCount() > 0 && LevelManager.I.CheckSceneInbounds(tile.position); }); }

    // Plays an "out" transition
    private IEnumerator OutTransition()
    {
        UI.I.preload.animator.SetTrigger("Out");

        yield return new WaitForSeconds(0.5f);

        UI.I.preload.Toggle(false);
        if (SceneManager.GetActiveScene().name != "Game") UI.I.ChangeScene("Game", false);
        else TransitionManager.I.TransitionOut<string>();

        outCoro = null;
    }

    // Debug commands //

    // Enable debug commands
    private void OnDebugEnable(InputValue ctx)
    {
        if (SceneManager.GetActiveScene().name != "Main Menu") return;
        canInputCommands = ctx.Get<float>() == 1f;

        if (!canInputCommands) { debugCommand = null; MainMenu.I.debug.text = null; }
    }

    // Confirm a command
    private bool DebugConfirm(string message = "Are you sure about that?")
    {
        if (confirmCommand != debugCommand)
        {
            MainMenu.I.ShowPopup(message);
            AudioManager.I.PlaySFX(AudioManager.uiDeny, 0.30f);
            MainMenu.I.debug.CrossFadeAlpha(0f, 1.25f, true);
            confirmCommand = $"{debugCommand}";
            debugCommand = null;
            return true;
        }
        
        confirmCommand = null;
        return false;
    }
}
