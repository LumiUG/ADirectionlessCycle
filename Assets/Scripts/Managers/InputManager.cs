using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static GameTile;
using static TransitionManager.Transitions;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    public Vector3Int latestMovement = Vector3Int.back;

    internal ObjectTypes latestTile = ObjectTypes.Hexagon;

    private bool isHoldingMovement = false;
    private bool isHoldingUndo = false;
    private Coroutine outCoro = null;
    private Coroutine movementCoro = null;
    private Coroutine undoCoro = null;
    private readonly float repeatMovementCD = 0.12f;
    private readonly float manualMovementCD = 0.12f;
    private float currentMovementCD = 0f;

    // Debug //
    private bool canInputCommands = false;
    private string confirmCommand = null;
    private string debugCommand = null;

    void Awake()
    {       
        // Singleton (InputManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Defaults
        latestMovement = Vector3Int.back;
    }

    
    void Update()
    {
        // Next level / Load level intermission
        if (TransitionManager.Instance.inTransition && UI.Instance.preload.self.activeSelf && Input.anyKeyDown)
        {
            if (outCoro != null) return;
            outCoro = StartCoroutine(OutTransition());
            return;
        }

        // Debug input
        if (!canInputCommands) return;

        // Write a command
        if (Input.anyKeyDown)
        {
            string keyboardInput = Input.inputString.Trim().ToLower();
            if (LevelManager.Instance.IsStringEmptyOrNull(keyboardInput)) {
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
            if (!GameManager.Instance.buildDebugMode)
            {
                GameManager.Instance.buildDebugMode = true;
                UI.Instance.global.SendMessage("I hope you know what you're doing.", 3);
            } else {
                GameManager.Instance.buildDebugMode = false;
                UI.Instance.global.SendMessage("Then so be it!", 3);
            }
            debugCommand = null;
        }

        // Secret title command
        if (debugCommand == "UDLRLRDL")
        {
            UI.Instance.global.SendMessage("Sneaky!", 3);
            debugCommand = null;
        }

        // Game completed flag
        if (debugCommand == "imacheater")
        {
            if (!GameManager.save.game.hasCompletedGame)
            {
                GameManager.save.game.hasCompletedGame = true;
                UI.Instance.global.SendMessage("Ready for some challenges?", 3);
            } else {
                GameManager.save.game.hasCompletedGame = false;
                UI.Instance.global.SendMessage("You've been redeemed.", 3);
            }
            debugCommand = null;
        }

        // Delete savedata and generate a new one
        else if (debugCommand == "zero")
        {
            if (DebugConfirm()) return;
            GameManager.Instance.DeleteSave();
            GameManager.Instance.CreateSave(true);
            UI.Instance.global.SendMessage("[ Game reset ]", 4);
            debugCommand = null;
        }

        // Delete savedata and generate a new one
        else if (debugCommand == "swap" && GameManager.Instance.buildDebugMode)
        {
            if (DebugConfirm()) return;
            GameManager.save.game.mechanics.hasSwapUpgrade = true;
            UI.Instance.global.SendMessage("[ New Ability Unlocked ]", 4);
            debugCommand = null;
        }

        if (debugCommand == null)
        {
            MainMenu.I.debug.CrossFadeAlpha(0f, 1.25f, true);
            AudioManager.Instance.PlaySFX(AudioManager.areaOverlap, 0.35f);
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
        if (!LevelManager.Instance.IsAllowedToPlay()) return;

        // Input prevention logic
        Vector3Int movement = Vector3Int.RoundToInt(ctx.Get<Vector2>());
        if (movement == Vector3Int.zero) { isHoldingMovement = false; return; }
        if (movement.x != 0 && movement.y != 0) return;

        // Tile vars
        isHoldingMovement = true;
        latestMovement = movement;

        // Move a single time
        if (MoveCDCheck(manualMovementCD) && !GameManager.save.preferences.repeatInput) return;
        if (!GameManager.save.preferences.repeatInput)
        {
            LevelManager.Instance.AddUndoFrame();
            LevelManager.Instance.ApplyGravity(movement);
            
            // New move CD
            currentMovementCD = Time.time;
            return;
        }

        // Repeat your movement
        if (movementCoro != null) StopCoroutine(movementCoro); 
        movementCoro = StartCoroutine(RepeatMovement(movement));
    }

    // Undo move
    private void OnUndo(InputValue ctx)
    {
        if (!LevelManager.Instance.IsAllowedToPlay()) return;
        bool holding = ctx.Get<float>() == 1f;
        isHoldingUndo = holding;

        // Undo latest move
        if (!holding) return;
        if (undoCoro != null) StopCoroutine(undoCoro); 
        undoCoro = StartCoroutine(RepeatUndo());
    }

    // Ping all areas
    private void OnShowOverlaps(InputValue ctx)
    {
        // Shows overlaps
        if (!LevelManager.Instance.IsAllowedToPlay() && !TransitionManager.Instance.inTransition) { LevelManager.Instance.ShowOverlaps(false); return; }
        LevelManager.Instance.ShowOverlaps(ctx.Get<float>() == 1f);
    }

    // Restart the level
    private void OnRestart()
    {
        if (!LevelManager.Instance.IsAllowedToPlay()) return;

        // Confirm restart screen
        if (GameManager.save.preferences.forceConfirmRestart)
        {
            UI.Instance.restart.Toggle(true);
            return;
        }

        // Transition in and out while restarting the level
        TransitionManager.Instance.TransitionIn<string>(Swipe, ActionRestart);
    }

    // Repeats a movement
    private IEnumerator RepeatMovement(Vector3Int direction, float speed = 0.005f)
    {
        while (direction == latestMovement && isHoldingMovement && LevelManager.Instance.IsAllowedToPlay())
        {
            if (!MoveCDCheck(repeatMovementCD))
            {
                // Move
                LevelManager.Instance.AddUndoFrame();
                LevelManager.Instance.ApplyGravity(direction);
                currentMovementCD = Time.time;
            }
            yield return new WaitForSeconds(speed);
        }
    }

    // Repeats undoing
    private IEnumerator RepeatUndo(float speed = 0.22f)
    {
        int performed = 0;
        while (isHoldingUndo && LevelManager.Instance.IsAllowedToPlay() && LevelManager.Instance.IsUndoQueueValid())
        {
            LevelManager.Instance.Undo();
            LevelManager.Instance.RemoveUndoFrame();
            yield return new WaitForSeconds(speed);

            // Speedup undoing gradually
            performed++;
            if (performed >= 5) { performed = 0; speed -= 0.015f; }
            if (speed <= 0.05f) speed = 0.05f; // speed cap
        }
    }

    // External (GameManager) //

    // Pause event
    private void OnPause()
    {
        if (GameManager.Instance.IsBadScene() || LevelManager.Instance.hasWon || DialogManager.Instance.inDialog || TransitionManager.Instance.inTransition || UI.Instance.restart.self.activeSelf) return;
        if (!UI.Instance.pause.self.activeSelf) LevelManager.Instance.PauseResumeGame(true);
        else LevelManager.Instance.PauseResumeGame(false);
    }

    // Level Editor (Editor) //

    // Places a tile
    private void OnEditorClickGrid()
    {
        if (!GameManager.Instance.IsEditor()) return;
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
        if (!GameManager.Instance.IsEditor()) return;
        if (Editor.I.popup.activeSelf) { Editor.I.popup.SetActive(false); return; }
        
        // Checks mouse position
        Vector3Int gridPos = Editor.I.GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.Instance.editor.self.activeSelf) return;

        // Tile validation and selection
        Editor.I.editingTile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!Editor.I.editingTile) Editor.I.editingTile = LevelManager.Instance.tilemapCustoms.GetTile<CustomTile>(gridPos);
        if (!Editor.I.editingTile) Editor.I.editingTile = LevelManager.Instance.tilemapEffects.GetTile<EffectTile>(gridPos); // Only arrow tiles
        if (!Editor.I.editingTile) { UI.Instance.global.SendMessage($"Invalid tile at position \"{gridPos}\""); return; }

        // Set popup position
        Vector3 screenPos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Editor.I.canvas.transform as RectTransform, screenPos, null, out Vector2 anchorPos);
        anchorPos = new Vector2(Math.Clamp(anchorPos.x, -710, 710), Math.Clamp(anchorPos.y, -202, 202));
        Editor.I.popupRect.anchoredPosition = anchorPos;
        
        Editor.I.popup.SetActive(true);
        Editor.I.ignoreUpdateEvent = true;

        // Custom tiles field (missing loading custom text automatically)
        if (LevelManager.Instance.typesCustomsList.Contains(Editor.I.editingTile.GetTileType())) {
            Editor.I.customInputField.interactable = true;
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
        if (!GameManager.Instance.IsEditor()) return;
        if (Editor.I.popup.activeSelf) { Editor.I.popup.SetActive(false); return; }

        Vector3Int gridPos = Editor.I.GetMousePositionOnGrid();
        if (gridPos == Vector3.back) return;

        GameTile tile = Editor.I.GetEditorTile(gridPos);
        if (tile) Editor.I.SelectListTile(tile, false);
    }

    // Toggles menu
    private void OnEscape()
    {
        if (!UI.Instance || !EventSystem.current) return;

        // Custom levels scene
        if (SceneManager.GetActiveScene().name == "Custom Levels") if (CustomLevels.I.popup.activeSelf) { CustomLevels.I.CloseLevelMenu(); return; }

        // Misc
        if (UI.Instance.restart.self.activeSelf) { UI.Instance.CloseConfirmRestart(); return; }

        // Editor scene
        if (!GameManager.Instance.IsEditor()) return;
        if (Editor.I.tileList.activeSelf) { Editor.I.ToggleTileMenu(); return; }
        if (Editor.I.popup.activeSelf) { Editor.I.popup.SetActive(false); return; }

        if (!UI.Instance.editor.self.activeSelf) UI.Instance.selectors.ChangeSelected(UI.Instance.editor.playtest, true);
        UI.Instance.editor.Toggle(!UI.Instance.editor.self.activeSelf);
    }

    // Save current level manually
    private void OnEditorSave()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf || Editor.I.popup.activeSelf) return;
        UI.Instance.LevelEditorExportLevel();
    }

    // Select deleting/placing tiles
    private void OnEditorDelete(InputValue ctx)
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf || Editor.I.popup.activeSelf) return;
        Editor.I.isPlacing = ctx.Get<float>() != 1f;
    }

    // Editor toggle tile menu
    private void OnEditorTileMenu()
    {
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.ToggleTileMenu();
    }

    // Editor select tiles 1/2/3/4
    private void OnEditorSelectOne()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(0);
    }
    private void OnEditorSelectTwo()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(1);
    }
    private void OnEditorSelectThree()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(2);
    }
    private void OnEditorSelectFour()
    {
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf) return;
        Editor.I.SelectMenuTile(3);
    }

    // Moving the level screen up/down/left/right
    private void OnEditorUp() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf || Editor.I.popup.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(0, -8));
        LevelManager.Instance.worldOffsetY += 8;
    }
    private void OnEditorDown() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf || Editor.I.popup.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(0, 8));
        LevelManager.Instance.worldOffsetY -= 8;
    }
    private void OnEditorLeft() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf || Editor.I.popup.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(14, 0));
        LevelManager.Instance.worldOffsetX -= 14;
    }
    private void OnEditorRight() 
    { 
        if (!GameManager.Instance.IsEditor() || UI.Instance.editor.self.activeSelf || Editor.I.popup.activeSelf) return;

        LevelManager.Instance.MoveTilemaps(new Vector3(-14, 0));
        LevelManager.Instance.worldOffsetX += 14;
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
        if (GameManager.Instance.IsBadScene() || LevelManager.Instance.isPaused) return;

        // Searches for a first valid NPC
        GameTile tl = null; // (garbage collector tysm)
        GameTile npc = LevelManager.Instance.GetCustomTiles().Find(
            npc =>
            {
                GameTile test = null;
                test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(1, 0, 0));
                if (!test) test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(-1, 0, 0));
                if (!test) test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(0, 1, 0));
                if (!test) test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position + new Vector3Int(0, -1, 0));
                if (!test) { test = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(npc.position); tl = test; } // inner
                if (test) return npc;
                else return false;
            }
        );
        
        // Gets the NPC and triggers it 
        if (npc) { LevelManager.Instance.tilemapCustoms.GetTile<NPCTile>(npc.position).Effect(tl); }
    }

    // Changes the only tile active's form (might cause issues in the future?)
    private void OnChangeForms()
    {
        if (!LevelManager.Instance.IsAllowedToPlay() || !GameManager.save.game.mechanics.hasSwapUpgrade) return;

        List<GameTile> count = GetPlayableObjects();
        if (count.Count > 1 || count.Count <= 0) return;

        LevelManager.Instance.RemoveTile(count[0]);
        if (count[0].GetTileType() == ObjectTypes.Hexagon) {
            LevelManager.Instance.PlaceTile(LevelManager.Instance.CreateTile(latestTile.ToString(), count[0].directions, count[0].position));
        }
        else {
            LevelManager.Instance.PlaceTile(LevelManager.Instance.CreateTile("Hexagon", count[0].directions, count[0].position));
            latestTile = count[0].GetTileType();
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

    internal List<GameTile> GetPlayableObjects() { return LevelManager.Instance.GetObjectTiles().FindAll(tile => { return tile.directions.GetActiveDirectionCount() > 0 && LevelManager.Instance.CheckSceneInbounds(tile.position); }); }

    // Plays an "out" transition
    private IEnumerator OutTransition()
    {
        UI.Instance.preload.animator.SetTrigger("Out");

        yield return new WaitForSeconds(0.5f);

        UI.Instance.preload.Toggle(false);
        if (SceneManager.GetActiveScene().name != "Game") UI.Instance.ChangeScene("Game", false);
        else TransitionManager.Instance.TransitionOut<string>();

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
    private bool DebugConfirm()
    {
        if (confirmCommand != debugCommand)
        {
            UI.Instance.global.SendMessage("Are you sure about that?", 2);
            AudioManager.Instance.PlaySFX(AudioManager.tileDeath, 0.30f);
            MainMenu.I.debug.CrossFadeAlpha(0f, 1.25f, true);
            confirmCommand = $"{debugCommand}";
            debugCommand = null;
            return true;
        }
        
        confirmCommand = null;
        return false;
    }

    // Actions //
    internal void ActionRestart(string _)
    {
        LevelManager.Instance.ReloadLevel();
        LevelManager.Instance.worldOffsetX = 0;
        LevelManager.Instance.worldOffsetY = 0;
        LevelManager.Instance.MoveTilemaps(LevelManager.Instance.originalPosition, true);
        TransitionManager.Instance.TransitionOut<string>(Swipe);
    }
}
