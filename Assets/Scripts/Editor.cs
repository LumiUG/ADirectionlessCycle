using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static GameTile;

public class Editor : MonoBehaviour
{
    private GameTile selectedTile;
    private (bool, bool, bool, bool) directionSet;
    private bool waitingForDirections = false;
    private bool isShiftHeld = false;
    private InputField inputField;

    private void Start() { inputField = transform.Find("Input Field").GetComponent<InputField>(); }

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
        else selectedTile = LevelManager.Instance.hexahedronTile;
    }

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
        GameTile tile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!tile) { Debug.LogWarning($"Invalid tile at position \"{gridPos}\""); return; }

        // Changes directions
        if (waitingForDirections) { Debug.LogWarning("Already waiting!"); return; }
        if (!isShiftHeld) StartCoroutine(WaitForDirection(tile));
        else tile.directions.pushable = !tile.directions.pushable;
    }

    // Toggles if the shift key is currently being held (passthrough value)
    private void OnShift() { isShiftHeld = !isShiftHeld; }

    // Confirm directions
    private void OnConfirm() { if (waitingForDirections) waitingForDirections = false; }

    // Toggles the export input field
    private void OnExport() { inputField.gameObject.SetActive(true); }

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
}
