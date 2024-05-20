using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static GameTile;

public class InputManager : MonoBehaviour
{
    // INGAME (LevelManager) //

    // Player movement
    private void OnMove(InputValue ctx)
    {
        if (!LevelManager.Instance.IsAllowedToPlay()) return;

        // Input prevention logic
        Vector3Int movement = Vector3Int.RoundToInt(ctx.Get<Vector2>());
        if (movement == Vector3Int.zero || (movement.x != 0 && movement.y != 0)) return;

        // Moves tiles
        LevelManager.Instance.latestMovement = movement;
        LevelManager.Instance.ApplyGravity(movement);
    }

    // Repeat last movement
    private void OnWait()
    {
        if (LevelManager.Instance.latestMovement == Vector3Int.zero || LevelManager.Instance.latestMovement == Vector3Int.back || !LevelManager.Instance.IsAllowedToPlay()) return;

        // Moves tiles using the user's latest movement
        LevelManager.Instance.ApplyGravity(LevelManager.Instance.latestMovement);
    }

    // Restart the level
    private void OnRestart()
    {
        if (!LevelManager.Instance.IsAllowedToPlay()) return;
        LevelManager.Instance.ReloadLevel();
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
        
        if (ctx.Get<float>() == 1f) Editor.I.isPlacing = false;
        else Editor.I.isPlacing = true;
    }
}
