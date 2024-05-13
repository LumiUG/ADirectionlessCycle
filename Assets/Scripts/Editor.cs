using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GameTile;

public class Editor : MonoBehaviour
{
    // Editor Default Settings //
    private Image previewImage;
    private GameTile selectedTile;
    private Coroutine multiClick = null;
    private (bool, bool, bool, bool) directionSet;
    private bool waitingForDirections = false;
    private bool isShiftHeld = false;
    private bool isPlacing = true;

    // Find preview image
    void Awake() { previewImage = transform.Find("Preview").GetComponent<Image>(); }

    // Set the preview image
    void FixedUpdate() { if (selectedTile) previewImage.sprite = selectedTile.tileSprite; }

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
    private void OnSelectEffect() { selectedTile = LevelManager.Instance.invertTile; }

    // Places a tile
    private void OnClickGrid()
    {
        // Checks if you are already multi-placing
        if (multiClick != null)
        {
            StopCoroutine(multiClick);
            multiClick = null;
            return;
        }

        // Multi placing tiles !!
        multiClick = StartCoroutine(MultiPlace());
    }

    // Changes a tile's properties (objects only)
    private void OnRightClickGrid()
    {
        // Checks mouse position
        Vector3Int gridPos = GetMousePositionOnGrid();
        if (gridPos == Vector3.back || UI.Instance.editor.self.activeSelf) return;

        // Selects the tile
        GameTile tile = LevelManager.Instance.tilemapObjects.GetTile<GameTile>(gridPos);
        if (!tile) { UI.Instance.global.SendMessage($"Invalid tile at position \"{gridPos}\""); return; }

        // Changes directions
        if (waitingForDirections) { UI.Instance.global.SendMessage($"Already Waiting!"); return; }
        if (!isShiftHeld) StartCoroutine(WaitForDirection(tile));
        else {
            UI.Instance.global.SendMessage("Pushable updated.");
            tile.directions.pushable = !tile.directions.pushable;
            tile.directions.UpdateSprites();
            LevelManager.Instance.RefreshObjectTile(tile);
        }
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

    // Places multiple tiles
    private IEnumerator MultiPlace()
    {
        while (true)
        {
            // Checks mouse position
            Vector3Int gridPos = GetMousePositionOnGrid();
            if (gridPos != Vector3.back)
            {
                // Places the tile
                if (isPlacing) EditorPlaceTile(gridPos);
                else EditorDeleteTile(gridPos);
            }

            // Waits and does another loop
            if (UI.Instance.editor.self.activeSelf) yield break;
            yield return new WaitForSeconds(0.01f);
        }
    }

    // Places a tile on the corresponding grid
    private void EditorPlaceTile(Vector3Int position)
    {
        if (selectedTile == null) return;

        // GameTile isThereATileThere = null;

        // Creates the tile
        GameTile tileToCreate = Instantiate(selectedTile);
        tileToCreate.position = position;

        // Sets the tile
        switch (selectedTile.GetTileType())
        {
            case ObjectTypes t when LevelManager.Instance.typesSolidsList.Contains(t):
                if (LevelManager.Instance.tilemapCollideable.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapCollideable.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToCollideableList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesAreas.Contains(t):
                if (LevelManager.Instance.tilemapWinAreas.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapWinAreas.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToWinAreasList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesHazardsList.Contains(t):
                if (LevelManager.Instance.tilemapHazards.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapHazards.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToHazardsList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesEffectsList.Contains(t):
                if (LevelManager.Instance.tilemapEffects.GetTile<GameTile>(position)) break;
                LevelManager.Instance.tilemapEffects.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToEffectsList(tileToCreate);
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
        if (!tile) tile = LevelManager.Instance.tilemapWinAreas.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.Instance.tilemapHazards.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.Instance.tilemapEffects.GetTile<GameTile>(position);
        if (tile) LevelManager.Instance.RemoveTile(tile);
    }

    // Waits for a direction to be set
    private IEnumerator WaitForDirection(GameTile tile)
    {
        UI.Instance.global.SendMessage($"Selected {tile.name}!");
        directionSet = (false, false, false, false);
        waitingForDirections = true;

        // Waits for enter to be pressed (confirming directions)
        while (waitingForDirections) { yield return new WaitForSeconds(0.01f); }

        // Sets the new tile directions (why do i have to refresh it???)
        tile.directions.SetNewDirections(directionSet.Item1, directionSet.Item2, directionSet.Item3, directionSet.Item4);
        LevelManager.Instance.RefreshObjectTile(tile);
        UI.Instance.global.SendMessage("Set new tile directions.");
    }

    // Toggles menu
    private void OnEscape() { if (UI.Instance) UI.Instance.editor.Toggle(!UI.Instance.editor.self.activeSelf); }

    // Select deleting/placing tiles
    private void OnDelete(InputValue ctx)
    {
        if (ctx.Get<float>() == 1f) isPlacing = false;
        else isPlacing = true;
    }
}
