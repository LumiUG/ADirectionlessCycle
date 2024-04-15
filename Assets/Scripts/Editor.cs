using System.Collections;
using UnityEngine;
using static GameTile;

public class Editor : MonoBehaviour
{
    private GameTile selectedTile;
    private bool isShiftHeld = false;

    // Player Input //

    // Changing tiles
    private void OnSelectWall() { selectedTile = LevelManager.Instance.wallTile; }
    private void OnSelectBox() { selectedTile = LevelManager.Instance.boxTile; }
    private void OnSelectCircle() { selectedTile = LevelManager.Instance.circleTile; }  
    private void OnSelectHex() { selectedTile = LevelManager.Instance.hexahedronTile; }
    private void OnSelectArea() { selectedTile = LevelManager.Instance.areaTile; }

    // Places a tile
    private void OnClickGrid()
    { 
        if (selectedTile == null) return;

        // Checks mouse position
        Vector3Int gridPos = GetMousePositionOnGrid();
        if (gridPos == Vector3.back) return;

        // Places the tile (add shift support)
        PlaceTile(gridPos);
    }

    // Changes a tile's properties (objects only)
    private void OnRightClickGrid()
    {
        // Checks mouse position
        Vector3Int gridPos = GetMousePositionOnGrid();
        if (gridPos == Vector3.back) return;

        // Selects the tile
        GameTile tile = LevelManager.Instance.tilemapCollideable.GetTile<GameTile>(gridPos);
        if (!tile) { Debug.LogWarning($"Invalid tile at position \"{gridPos}\""); return; }

        // Changes directions
        if (!isShiftHeld) StartCoroutine(WaitForDirection());
        else tile.directions.pushable = !tile.directions.pushable;
    }

    // Toggles if the shift key is currently being held (passthrough value)
    private void OnShift() { isShiftHeld = !isShiftHeld; }

    // Returns the mouse position on the playable grid
    private Vector3Int GetMousePositionOnGrid()
    {
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = LevelManager.Instance.tilemapCollideable.WorldToCell(worldPoint);

        if (!LevelManager.Instance.CheckSceneInbounds(gridPos)) return Vector3Int.back;
        return gridPos;
    }

    // Places a tile on the corresponding grid
    private void PlaceTile(Vector3Int position)
    {
        // Creates the tile
        GameTile tileToCreate = Instantiate(selectedTile);
        tileToCreate.position = position;

        // Sets the tile
        switch (selectedTile.GetTileType())
        {
            case ObjectTypes.Wall:
                LevelManager.Instance.tilemapCollideable.SetTile(tileToCreate.position, tileToCreate);
                break;
            case ObjectTypes.Area:
                LevelManager.Instance.tilemapOverlaps.SetTile(tileToCreate.position, tileToCreate);
                break;
            default:
                LevelManager.Instance.tilemapObjects.SetTile(tileToCreate.position, tileToCreate);
                break;
        }
    }

    // Waits for a direction to be set
    private IEnumerator WaitForDirection()
    {
        yield return new WaitForSeconds(0.01f);
    }
}
