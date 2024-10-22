using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static GameTile;

public class Editor : MonoBehaviour
{
    // Editor Default Settings //
    [HideInInspector] public static Editor I;
    [HideInInspector] public bool ignoreUpdateEvent = false;

    internal SpriteRenderer spriteRenderer;
    internal Coroutine multiClick = null;
    internal bool isPlacing = true;
    internal GameTile editingTile = null;
    internal GameTile tileToPlace;

    private Tilemap editorTilemap;
    private Sprite directionSprite;
    private Sprite deletionSprite;
    private Sprite arrowSprite;
    private Sprite colorArrowSprite;
    private readonly List<string> listStrings = new() { "Solids", "Objects", "Areas", "Hazards", "Effects", "Customs" };
    private readonly List<List<GameTile>> listVars = new();

    // UI //
    private GameObject tileList;
    private Image previewImage;
    internal Toggle upToggle;
    internal Toggle downToggle;
    internal Toggle leftToggle;
    internal Toggle rightToggle;
    internal Toggle pushableToggle;
    internal InputField customInputField;

    // Find preview image
    void Awake()
    {
        I = this; // No persistence!
        editorTilemap = GameObject.Find("Editor Tilemap").GetComponent<Tilemap>();
        spriteRenderer = GameObject.Find("Tilemap Preview").GetComponent<SpriteRenderer>();
        directionSprite = Resources.Load<Sprite>("Sprites/Direction");
        deletionSprite = Resources.Load<Sprite>("Sprites/Non Pushable");
        arrowSprite = Resources.Load<Sprite>("Sprites/Tiles/ColorArrows");
        colorArrowSprite = Resources.Load<Sprite>("Sprites/Tiles/Arrows");

        // Tile info
        previewImage = transform.Find("Preview").GetComponent<Image>();
        tileList = transform.Find("Tile List").gameObject;

        Transform directions = transform.Find("Tile Information").Find("Directions");
        upToggle = directions.Find("Up").GetComponent<Toggle>();
        downToggle = directions.Find("Down").GetComponent<Toggle>();
        leftToggle = directions.Find("Left").GetComponent<Toggle>();
        rightToggle = directions.Find("Right").GetComponent<Toggle>();
        pushableToggle = directions.Find("Pushable").GetComponent<Toggle>();
        customInputField = directions.Find("Text").GetComponent<InputField>();

        // Default tile
        tileToPlace = LevelManager.Instance.wallTile;

        // Populate tile list
        GameObject editorTile = Resources.Load<GameObject>("Prefabs/Editor Tile");
        listVars.Add(new() { LevelManager.Instance.wallTile, LevelManager.Instance.antiwallTile });
        listVars.Add(new() { LevelManager.Instance.boxTile, LevelManager.Instance.circleTile, LevelManager.Instance.hexagonTile, LevelManager.Instance.mimicTile });
        listVars.Add(new() { LevelManager.Instance.areaTile, LevelManager.Instance.inverseAreaTile, LevelManager.Instance.outboundAreaTile });
        listVars.Add(new() { LevelManager.Instance.hazardTile, LevelManager.Instance.voidTile });
        listVars.Add(new() { LevelManager.Instance.invertTile, LevelManager.Instance.arrowTile, LevelManager.Instance.negativeArrowTile, LevelManager.Instance.fragmentTile, LevelManager.Instance.orbTile });
        listVars.Add(new() { LevelManager.Instance.levelTile, LevelManager.Instance.hologramTile, LevelManager.Instance.npcTile });

        // Loops for every tile type
        for (int i = 0; i < listStrings.Count; i++)
        {
            int offset = -150;
            // Fills every tile
            foreach (GameTile tile in listVars[i])
            {
                GameObject currentTile = Instantiate(editorTile, tileList.transform.Find(listStrings[i]));
                currentTile.GetComponent<Button>().onClick.AddListener(delegate { tileToPlace = tile; });

                // Sprite (and arrows as exceptions)
                if (tile.GetTileType() == ObjectTypes.Arrow) currentTile.GetComponent<Image>().sprite = colorArrowSprite;
                else if (tile.GetTileType() == ObjectTypes.NegativeArrow) currentTile.GetComponent<Image>().sprite = arrowSprite;
                else currentTile.GetComponent<Image>().sprite = tile.tileSprite;

                // Position and offset
                RectTransform rect = currentTile.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + offset);
                currentTile.name = tile.name;
                offset -= 175;
            }
        }
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
        if (isPlacing) {
            // Custom handling for arrow tiles.
            if (tileToPlace.GetTileType() == ObjectTypes.Arrow) spriteRenderer.sprite = colorArrowSprite;
            else if (tileToPlace.GetTileType() == ObjectTypes.NegativeArrow) spriteRenderer.sprite = arrowSprite;
            else spriteRenderer.sprite = previewImage.sprite;
        }
        else spriteRenderer.sprite = deletionSprite;

        // Move mouse selector (on tilemap)
        if (UI.Instance.editor.self.activeSelf || tileList.activeSelf) return;
        Vector3Int mousePos = GetMousePositionOnGrid();
        if (mousePos == Vector3.back) return;

        mousePos -= new Vector3Int(LevelManager.Instance.worldOffsetX, LevelManager.Instance.worldOffsetY);
        spriteRenderer.transform.position = editorTilemap.GetCellCenterWorld(mousePos);
    }

    // Returns the mouse position on the playable grid
    internal Vector3Int GetMousePositionOnGrid()
    {
        if (LevelManager.Instance.currentLevel == null) return Vector3Int.back;
        
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
            if (UI.Instance.editor.self.activeSelf || tileList.activeSelf) yield break;

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

        // Creates the tile (this creates a tile every frame the button is held! very bad!)
        GameTile tileToCreate = Instantiate(tileToPlace);
        tileToCreate.position = position;
        tileToCreate.PrepareTile();

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

            case ObjectTypes t when LevelManager.Instance.typesCustomsList.Contains(t):
                if (LevelManager.Instance.tilemapCustoms.GetTile<CustomTile>(position)) break;
                CustomTile custom = (CustomTile)tileToCreate;
                LevelManager.Instance.tilemapCustoms.SetTile(custom.position, custom);
                LevelManager.Instance.AddToCustomsList(custom);
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
        if (!tile) tile = LevelManager.Instance.tilemapCustoms.GetTile<GameTile>(position);
        if (tile) LevelManager.Instance.RemoveTile(tile);
    }

    // Updates the selected tile's custom text
    public void UpdateCustomText(string text)
    {
        if (!editingTile) return;

        // stupid ren was here
        if (!LevelManager.Instance.typesCustomsList.Contains(editingTile.GetTileType())) return;

        // Get the real tile that you can edit
        var existingRule = LevelManager.Instance.customTileInfo.Find(rule => { return rule.position == editingTile.position; });
        if (existingRule != null) existingRule.text = text;
        else LevelManager.Instance.customTileInfo.Add(new(editingTile.position, text));
        UI.Instance.global.SendMessage($"Set custom text to \"{text}\".", 2.25f);
        customInputField.interactable = false;
    }

    // Updates the selected tile's pushable
    public void UpdatePushable(bool value)
    {
        if (!editingTile || !editingTile.directions.editorPushable || ignoreUpdateEvent) return;
        editingTile.directions.pushable = value;
        editingTile.directions.UpdateSprites();
        LevelManager.Instance.RefreshObjectTile(editingTile);
    }

    public void UpdateDirection(Toggle toggle)
    {
        if (!editingTile || !editingTile.directions.editorDirections || ignoreUpdateEvent) return;

        // Direction thing (awful)
        switch(toggle.name)
        {
            case "Up":
                if (editingTile.directions.GetActiveDirectionCount() + -Convert.ToInt32(editingTile.directions.up) < editingTile.directions.editorMinimumDirections) { ToggleToggle(toggle); return; } 
                editingTile.directions.SetNewDirections(!editingTile.directions.up, editingTile.directions.down, editingTile.directions.left, editingTile.directions.right);
                break;
            case "Down":
                if (editingTile.directions.GetActiveDirectionCount() + -Convert.ToInt32(editingTile.directions.down) < editingTile.directions.editorMinimumDirections) { ToggleToggle(toggle); return; } 
                editingTile.directions.SetNewDirections(editingTile.directions.up, !editingTile.directions.down, editingTile.directions.left, editingTile.directions.right);
                break;
            case "Left":
                if (editingTile.directions.GetActiveDirectionCount() + -Convert.ToInt32(editingTile.directions.left) < editingTile.directions.editorMinimumDirections) { ToggleToggle(toggle); return; } 
                editingTile.directions.SetNewDirections(editingTile.directions.up, editingTile.directions.down, !editingTile.directions.left, editingTile.directions.right);
                break;
            case "Right":
                if (editingTile.directions.GetActiveDirectionCount() + -Convert.ToInt32(editingTile.directions.right) < editingTile.directions.editorMinimumDirections) { ToggleToggle(toggle); return; } 
                editingTile.directions.SetNewDirections(editingTile.directions.up, editingTile.directions.down, editingTile.directions.left, !editingTile.directions.right);
                break;
        }
    
        // Refresh tile
        if (editingTile.GetTileType() == ObjectTypes.Arrow || editingTile.GetTileType() == ObjectTypes.NegativeArrow) LevelManager.Instance.RefreshEffectTile(editingTile);
        else LevelManager.Instance.RefreshObjectTile(editingTile);
    }

    // Turn on/off a toggle without evoking an event
    private void ToggleToggle(Toggle toggle)
    {
        ignoreUpdateEvent = true;
        toggle.isOn = !toggle.isOn;
        ignoreUpdateEvent = false;
    }

    // Toggles the tile list on/off
    public void ToggleTileMenu()
    {
        if (tileList.activeSelf) tileList.SetActive(false);
        else  tileList.SetActive(true);
    }
}
