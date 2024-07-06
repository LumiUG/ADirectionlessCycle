using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static GameTile;

public class Editor : MonoBehaviour
{
    // Editor Default Settings //
    public static Editor I;
    private Tilemap editorTilemap;
    private SpriteRenderer tilemapRenderer;
    private Sprite directionSprite;
    private Sprite deletionSprite;
    internal Coroutine multiClick = null;
    internal bool isPlacing = true;
    internal GameTile editingTile = null;
    internal GameTile tileToPlace;

    // UI //
    private Image previewImage;
    internal Toggle upToggle;
    internal Toggle downToggle;
    internal Toggle leftToggle;
    internal Toggle rightToggle;
    internal Toggle pushableToggle;

    // Find preview image
    void Awake()
    {
        I = this; // No persistence!
        editorTilemap = GameObject.Find("Editor Tilemap").GetComponent<Tilemap>();
        tilemapRenderer = GameObject.Find("Tilemap Preview").GetComponent<SpriteRenderer>();
        directionSprite = Resources.Load<Sprite>("Sprites/Direction");
        deletionSprite = Resources.Load<Sprite>("Sprites/Non Pushable");

        // Tile info
        previewImage = transform.Find("Preview").GetComponent<Image>();
        Transform directions = transform.Find("Tile Information").Find("Directions");
        upToggle = directions.Find("Up").GetComponent<Toggle>();
        downToggle = directions.Find("Down").GetComponent<Toggle>();
        leftToggle = directions.Find("Left").GetComponent<Toggle>();
        rightToggle = directions.Find("Right").GetComponent<Toggle>();
        pushableToggle = directions.Find("Pushable").GetComponent<Toggle>();
    }

    void OnDisable() { I = null; }

    // Set the preview image
    void Update()
    {
        if (!tileToPlace) return;

        // Preview sprite (top-right)
        if (tileToPlace.GetTileType() == ObjectTypes.Arrow || tileToPlace.GetTileType() == ObjectTypes.NegativeArrow) previewImage.sprite = directionSprite;
        else previewImage.sprite = tileToPlace.tileSprite;

        // Preview sprite (on tilemap)
        Vector3Int mousePos = GetMousePositionOnGrid();
        if (mousePos == Vector3.back) return;

        tilemapRenderer.transform.position = editorTilemap.GetCellCenterWorld(mousePos);
        if (isPlacing) tilemapRenderer.sprite = previewImage.sprite;
        else tilemapRenderer.sprite = deletionSprite;
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
        if (tileToPlace == null) return;

        // Creates the tile
        GameTile tileToCreate = Instantiate(tileToPlace);
        tileToCreate.position = position;

        // Sets the tile
        switch (tileToPlace.GetTileType())
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

    // Updates the selected tile's pushable
    public void UpdatePushable(bool value)
    {
        if (!editingTile) return;
        editingTile.directions.pushable = value;
        editingTile.directions.UpdateSprites();
        LevelManager.Instance.RefreshObjectTile(editingTile);
    }

    public void UpdateDirection(int direction)
    {
        if (!editingTile) return;

        // Direction thing (awful)
        switch(direction)
        {
            case 1:
                editingTile.directions.up = !editingTile.directions.up;
                break;
            case 2:
                editingTile.directions.down = !editingTile.directions.down;
                break;
            case 3:
                editingTile.directions.left = !editingTile.directions.left;
                break;
            case 4:
                editingTile.directions.right = !editingTile.directions.right;
                break;
        }
    
        // Sets the new tile directions (why do i have to refresh it???)
        editingTile.directions.SetNewDirections();
        if (editingTile.GetTileType() == ObjectTypes.Arrow || editingTile.GetTileType() == ObjectTypes.NegativeArrow) LevelManager.Instance.RefreshEffectTile(editingTile);
        else LevelManager.Instance.RefreshObjectTile(editingTile);
    }
}
