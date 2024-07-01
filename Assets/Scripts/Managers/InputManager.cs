using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static GameTile;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;
    public Vector3Int latestMovement = Vector3Int.back;

    private bool isHolding = false;
    private Coroutine movementCoro = null;
    private readonly float repeatMovementCD = 0.12f;
    private readonly float manualMovementCD = 0.12f;
    private float currentMovementCD = 0f;

    void Awake()
    {       
        // Singleton without persistence (GameManager already declares it)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Defaults
        latestMovement = Vector3Int.back;
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
        if (movement == Vector3Int.zero) { isHolding = false; return; }
        if (movement.x != 0 && movement.y != 0) return;

        // Tile vars
        isHolding = true;
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
    private void OnUndo()
    {
        if (!LevelManager.Instance.IsAllowedToPlay() || !LevelManager.Instance.IsUndoQueueValid()) return;
        LevelManager.Instance.Undo();
        LevelManager.Instance.RemoveUndoFrame();
    }

    // Ping all areas (FYI, this is horrible.)
    private void OnPingAreas(InputValue ctx)
    {
        LevelManager.Instance.PingAllAreas(ctx.Get<float>() == 1f);
    }

    // Restart the level
    private void OnRestart()
    {
        if (!LevelManager.Instance.IsAllowedToPlay()) return;
        LevelManager.Instance.ReloadLevel();
    }

    // Repeats a movement
    private IEnumerator RepeatMovement(Vector3Int direction, float speed = 0.005f)
    {
        while (direction == latestMovement && isHolding && !LevelManager.Instance.hasWon)
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

    // External (GameManager) //

    // Pause event
    private void OnPause()
    {
        if (GameManager.Instance.IsBadScene() || LevelManager.Instance.hasWon) return;
        if (!UI.Instance.pause.self.activeSelf) LevelManager.Instance.PauseResumeGame(true);
        else LevelManager.Instance.PauseResumeGame(false);
    }

    // Level Editor (Editor) //

    // Changing tiles
    private void OnEditorSelectWall()
    {
        if (!GameManager.Instance.IsEditor()) return;

        if (Editor.I.waitingForDirections) Editor.I.directionSet = (!Editor.I.directionSet.Item1, Editor.I.directionSet.Item2, Editor.I.directionSet.Item3, Editor.I.directionSet.Item4);
        else Editor.I.selectedTile = LevelManager.Instance.wallTile;
    }

    private void OnEditorSelectBox()
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        if (Editor.I.waitingForDirections) Editor.I.directionSet = (Editor.I.directionSet.Item1, !Editor.I.directionSet.Item2, Editor.I.directionSet.Item3, Editor.I.directionSet.Item4);
        else Editor.I.selectedTile = LevelManager.Instance.boxTile;
    }

    private void OnEditorSelectCircle()
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        if (Editor.I.waitingForDirections) Editor.I.directionSet = (Editor.I.directionSet.Item1, Editor.I.directionSet.Item2, !Editor.I.directionSet.Item3, Editor.I.directionSet.Item4);
        else Editor.I.selectedTile = LevelManager.Instance.circleTile; 
    }  

    private void OnEditorSelectHex() 
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        if (Editor.I.waitingForDirections) Editor.I.directionSet = (Editor.I.directionSet.Item1, Editor.I.directionSet.Item2, Editor.I.directionSet.Item3, !Editor.I.directionSet.Item4);
        else Editor.I.selectedTile = LevelManager.Instance.hexagonTile;
    }

    private void OnEditorSelectArea()
    {
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.areaTile;
    }
    private void OnEditorSelectInverseArea() 
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.inverseAreaTile; 
    }
    private void OnEditorSelectHazard() 
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.hazardTile; 
    }
    private void OnEditorSelectInvert() 
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.invertTile; 
    }
    private void OnEditorSelectArrow() 
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.arrowTile; 
    }
    private void OnEditorSelectNegativeArrow() 
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.negativeArrowTile; 
    }
    private void OnEditorSelectMimic() 
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.selectedTile = LevelManager.Instance.mimicTile; 
    }

    // Places a tile
    private void OnEditorClickGrid()
    {
        if (!GameManager.Instance.IsEditor()) return;
        
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

    // Changes a tile's properties (objects only)
    private void OnEditorRightClickGrid()
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        // Checks mouse position
        Vector3Int gridPos = Editor.I.GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.Instance.editor.self.activeSelf) return;

        // Selects the tile
        GameTile tile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!tile) tile = LevelManager.Instance.tilemapEffects.GetTile<EffectTile>(gridPos); // Only arrow tiles
        if (!tile) { UI.Instance.global.SendMessage($"Invalid tile at position \"{gridPos}\""); return; }

        // Changes directions
        if (Editor.I.waitingForDirections) { UI.Instance.global.SendMessage($"Already Waiting!"); return; }
        if (!Editor.I.isShiftHeld) StartCoroutine(Editor.I.WaitForDirection(tile));
        else {
            if (tile.GetTileType() == ObjectTypes.Arrow || tile.GetTileType() == ObjectTypes.NegativeArrow) return;

            // Update pushable
            UI.Instance.global.SendMessage("Pushable updated.");
            tile.directions.pushable = !tile.directions.pushable;
            tile.directions.UpdateSprites();
            LevelManager.Instance.RefreshObjectTile(tile);
        }
    }

    // Toggles if the shift key is currently being held (passthrough value)
    private void OnEditorShift()
    { 
        if (!GameManager.Instance.IsEditor()) return;
        Editor.I.isShiftHeld = !Editor.I.isShiftHeld;
    }

    // Confirm directions
    private void OnEditorConfirm()
    { 
        if (!GameManager.Instance.IsEditor()) return;
        if (Editor.I.waitingForDirections) Editor.I.waitingForDirections = false;
    }

    // Toggles menu
    private void OnEditorEscape()
    {
        if (!GameManager.Instance.IsEditor()) return;
        if (!UI.Instance) return;

        if (!UI.Instance.editor.self.activeSelf) EventSystem.current.SetSelectedGameObject(UI.Instance.editor.playtest);
        UI.Instance.editor.Toggle(!UI.Instance.editor.self.activeSelf);
    }

    // Select deleting/placing tiles
    private void OnEditorDelete(InputValue ctx)
    {
        if (!GameManager.Instance.IsEditor()) return;
        
        Editor.I.isPlacing = ctx.Get<float>() != 1f;
    }
}
