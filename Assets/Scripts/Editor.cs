using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static GameTile;

public class Editor : MonoBehaviour
{
    // Editor Default Settings //
    public static Editor I;
    private Image previewImage;
    private Sprite directionSprite;
    internal Coroutine multiClick = null;
    internal (bool, bool, bool, bool) directionSet;
    internal bool waitingForDirections = false;
    internal bool isPlacing = true;
    internal bool isShiftHeld = false;
    internal GameTile selectedTile;

    // Find preview image
    void Awake()
    {
        I = this; // No persistence!
        previewImage = transform.Find("Preview").GetComponent<Image>();
        directionSprite = Resources.Load<Sprite>("Sprites/Direction");
    }

    void OnDisable() { I = null; }

    // Set the preview image
    void FixedUpdate()
    {
        if (!selectedTile) return;

        if (selectedTile.GetTileType() == ObjectTypes.Arrow || selectedTile.GetTileType() == ObjectTypes.NegativeArrow) previewImage.sprite = directionSprite;
        else previewImage.sprite = selectedTile.tileSprite;
    }

    // Returns the mouse position on the playable grid
    internal Vector3Int GetMousePositionOnGrid()
    {
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = LevelManager.Instance.tilemapCollideable.WorldToCell(worldPoint);

        if (!LevelManager.Instance.CheckSceneInbounds(gridPos)) return Vector3Int.back;
        return gridPos;
    }

    // Places multiple tiles
    internal IEnumerator MultiPlace()
    {
        while (true)
        {
            if (UI.Instance.editor.self.activeSelf) yield break;

            // Checks mouse position
            Vector3Int gridPos = GetMousePositionOnGrid();
            if (gridPos != Vector3.back)
            {
                // Places the tile
                if (isPlacing) EditorPlaceTile(gridPos);
                else EditorDeleteTile(gridPos);
            }

            // Waits and does another loop
            yield return new WaitForSeconds(0.005f);
        }
    }

    // Places a tile on the corresponding grid
    private void EditorPlaceTile(Vector3Int position)
    {
        if (selectedTile == null) return;

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
    internal IEnumerator WaitForDirection(GameTile tile)
    {
        UI.Instance.global.SendMessage($"Selected {tile.name}!");
        directionSet = (false, false, false, false);
        waitingForDirections = true;

        // Waits for enter to be pressed (confirming directions)
        while (waitingForDirections) { yield return new WaitForSeconds(0.01f); }

        // Sets the new tile directions (why do i have to refresh it???)
        tile.directions.SetNewDirections(directionSet.Item1, directionSet.Item2, directionSet.Item3, directionSet.Item4);
        if (tile.GetTileType() == ObjectTypes.Arrow || tile.GetTileType() == ObjectTypes.NegativeArrow) LevelManager.Instance.RefreshEffectTile(tile);
        else LevelManager.Instance.RefreshObjectTile(tile);
        UI.Instance.global.SendMessage("Set new tile directions.");
    }
}
