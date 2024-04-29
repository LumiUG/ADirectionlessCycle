using System.Collections;
using UnityEngine;
using static GameTile;

public class Editor : MonoBehaviour
{
    private GameTile selectedTile;
    private (bool, bool, bool, bool) directionSet;
    private bool waitingForDirections = false;
    private bool isShiftHeld = false;
    private bool isPlacing = true;

    // Player Input //

    // Changing tiles
    private void OnSelectWall()
    {
        if (waitingForDirections) directionSet = (!directionSet.Item1, directionSet.Item2, directionSet.Item3, directionSet.Item4);
        else selectedTile = LevelManager.Instance.wallTile;
    }

    private void OnSelectBox()
    {
        if (waitingForDirections) directionSet = (directionSet.Item1, !directionSet.Item2, directionSet.Item3, directionSet.Item4);
        else selectedTile = LevelManager.Instance.boxTile;
    }

    private void OnSelectCircle()
    {
        if (waitingForDirections) directionSet = (directionSet.Item1, directionSet.Item2, !directionSet.Item3, directionSet.Item4);
        else selectedTile = LevelManager.Instance.circleTile; 
    }  

    private void OnSelectHex() 
    {
        if (waitingForDirections) directionSet = (directionSet.Item1, directionSet.Item2, directionSet.Item3, !directionSet.Item4);
        else selectedTile = LevelManager.Instance.hexagonTile;
    }

    private void OnSelectArea() { selectedTile = LevelManager.Instance.areaTile; }
    private void OnSelectInverseArea() { selectedTile = LevelManager.Instance.inverseAreaTile; }
    private void OnSelectHazard() { selectedTile = LevelManager.Instance.hazardTile; }

    // Places a tile
    private void OnClickGrid()
    { 
        if (selectedTile == null) return;

        // Checks mouse position
        Vector3Int gridPos = GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.Instance.editor.self.activeSelf) return;

        // Places the tile (add shift support)
        if (isPlacing) EditorPlaceTile(gridPos);
        else EditorDeleteTile(gridPos);

    }

    // Changes a tile's properties (objects only)
    private void OnRightClickGrid()
    {
        // Checks mouse position
        Vector3Int gridPos = GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.Instance.editor.self.activeSelf) return;

        // Selects the tile
        GameTile tile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!tile) { Debug.LogWarning($"Invalid tile at position \"{gridPos}\""); return; }

        // Changes directions
        if (waitingForDirections) { Debug.LogWarning("Already waiting!"); return; }
        if (!isShiftHeld) StartCoroutine(WaitForDirection(tile));
        else { tile.directions.pushable = !tile.directions.pushable; tile.directions.UpdateSprites(); }
    }

    // Toggles if the shift key is currently being held (passthrough value)
    private void OnShift() { isShiftHeld = !isShiftHeld; }

    // Confirm directions
    private void OnConfirm() { if (waitingForDirections) waitingForDirections = false; }

    // Returns the mouse position on the playable grid
    private Vector3Int GetMousePositionOnGrid()
    {
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = LevelManager.Instance.tilemapCollideable.WorldToCell(worldPoint);

        if (!LevelManager.Instance.CheckSceneInbounds(gridPos)) return Vector3Int.back;
        return gridPos;
    }

    // Places a tile on the corresponding grid
    private void EditorPlaceTile(Vector3Int position)
    {
        // GameTile isThereATileThere = null;

        // Creates the tile
        GameTile tileToCreate = Instantiate(selectedTile);
        tileToCreate.position = position;

        // Sets the tile
        switch (selectedTile.GetTileType())
        {
            case ObjectTypes.Wall:
                if (LevelManager.Instance.tilemapCollideable.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapCollideable.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToCollideableList(tileToCreate);
                break;

            case ObjectTypes.Area:
            case ObjectTypes.InverseArea:
            case ObjectTypes.Hazard:
                if (LevelManager.Instance.tilemapOverlaps.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapOverlaps.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToOverlapList(tileToCreate);
                break;

            default:
                if (LevelManager.Instance.tilemapObjects.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapObjects.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToObjectList(tileToCreate);
                break;
        }
    }

    // Deletes a tile from the corresponding grid (holy shit kill me)
    private void EditorDeleteTile(Vector3Int position)
    {
        GameTile tile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.Instance.tilemapCollideable.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.Instance.tilemapOverlaps.GetTile<GameTile>(position);
        if (tile) LevelManager.Instance.RemoveTile(tile);
    }

    // Waits for a direction to be set
    private IEnumerator WaitForDirection(GameTile tile)
    {
        Debug.Log($"Selected {tile.name}!");
        directionSet = (false, false, false, false);
        waitingForDirections = true;

        // Waits for enter to be pressed (confirming directions)
        while (waitingForDirections) { yield return new WaitForSeconds(0.01f); }

        // Sets the new tile directions (why do i have to refresh it???)
        tile.directions.SetNewDirections(directionSet.Item1, directionSet.Item2, directionSet.Item3, directionSet.Item4);
        LevelManager.Instance.tilemapObjects.SetTile(tile.position, null);
        LevelManager.Instance.tilemapObjects.SetTile(tile.position, tile);
        LevelManager.Instance.tilemapObjects.RefreshTile(tile.position);
        Debug.Log("Set new tile directions.");
    }

    // Toggles menu
    private void OnEscape() { if (UI.Instance) UI.Instance.editor.Toggle(!UI.Instance.editor.self.activeSelf); }

    // Select deleting tiles
    private void OnDelete() { isPlacing = false; }

    // Select placing tiles
    private void OnPlace() { isPlacing = true; }
}
