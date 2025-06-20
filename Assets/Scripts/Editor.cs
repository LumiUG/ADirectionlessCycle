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
    public List<Image> menuTiles = new(capacity: 4);
    public List<GameObject> menuSelectors = new(capacity: 4);
    public Canvas canvas;

    internal SpriteRenderer spriteRenderer;
    internal Coroutine multiClick = null;
    internal bool isPlacing = true;
    internal GameTile editingTile = null;
    internal ObjectTypes tileToPlace;
    internal RectTransform popupRect;

    private Tilemap editorTilemap;
    private Sprite deletionSprite;
    private Sprite badArrowSprite;
    private Sprite colorArrowSprite;
    private readonly List<string> listStrings = new() { "Solids", "Objects", "Areas", "Hazards", "Effects", "Customs" };
    private readonly List<List<GameTile>> listVars = new();
    private readonly List<ICommand> commandHistory = new();
    private int selectedTileIndex = 0;

    // UI //
    public Text coords; 
    internal GameObject popup;
    internal GameObject tileList;
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
        deletionSprite = Resources.Load<Sprite>("Sprites/Non Pushable");
        colorArrowSprite = Resources.Load<Sprite>("Sprites/Tiles/ColorArrows");
        badArrowSprite = Resources.Load<Sprite>("Sprites/Tiles/BadArrows");

        // Tile info
        tileList = transform.Find("Tile List").gameObject;

        popup = transform.Find("Tile Information").gameObject;
        popupRect = popup.GetComponent<RectTransform>();
        Transform directions = popup.transform.Find("Directions");
        upToggle = directions.Find("Up").GetComponent<Toggle>();
        downToggle = directions.Find("Down").GetComponent<Toggle>();
        leftToggle = directions.Find("Left").GetComponent<Toggle>();
        rightToggle = directions.Find("Right").GetComponent<Toggle>();
        pushableToggle = directions.Find("Pushable").GetComponent<Toggle>();
        customInputField = directions.Find("Text").GetComponent<InputField>();

        // Default tile
        tileToPlace = GameManager.save.preferences.editorTiles[0];

        // Populate tile list
        GameObject editorTile = Resources.Load<GameObject>("Prefabs/Editor Tile");
        listVars.Add(new() { LevelManager.I.wallTile, LevelManager.I.antiwallTile });
        listVars.Add(new() { LevelManager.I.boxTile, LevelManager.I.circleTile, LevelManager.I.hexagonTile });
        listVars.Add(new() { LevelManager.I.areaTile, LevelManager.I.inverseAreaTile, LevelManager.I.outboundAreaTile });
        listVars.Add(new() { LevelManager.I.hazardTile, LevelManager.I.voidTile });
        listVars.Add(new() { LevelManager.I.invertTile, LevelManager.I.pullTile, LevelManager.I.arrowTile, LevelManager.I.negativeArrowTile });
        listVars.Add(new() { LevelManager.I.levelTile, LevelManager.I.hologramTile, LevelManager.I.npcTile, LevelManager.I.maskTile });
        if (GameManager.I.editormimic || GameManager.I.IsDebug()) listVars[1].Add(LevelManager.I.mimicTile);
        if (GameManager.I.IsDebug())
        {
            listVars[4].Add(LevelManager.I.fragmentTile);
            listVars[4].Add(LevelManager.I.orbTile);
        }

        // Loops for every tile type
        for (int i = 0; i < listStrings.Count; i++)
        {
            int offset = -150;
            // Fills every tile
            foreach (GameTile tile in listVars[i])
            {
                GameObject currentTile = Instantiate(editorTile, tileList.transform.Find(listStrings[i]));

                Button btn = currentTile.GetComponent<Button>();
                btn.onClick.AddListener(delegate { SelectListTile(tile); });
                ColorBlock cVar = btn.colors;
                cVar.highlightedColor = Color.gray;
                cVar.pressedColor = Color.gray;
                btn.colors = cVar;

                // Sprite (and arrows as exceptions)
                if (tile.GetTileType() == ObjectTypes.Arrow) currentTile.GetComponent<Image>().sprite = colorArrowSprite;
                else if (tile.GetTileType() == ObjectTypes.NegativeArrow) currentTile.GetComponent<Image>().sprite = badArrowSprite;
                else currentTile.GetComponent<Image>().sprite = tile.tileSprite;

                // Position and offset
                RectTransform rect = currentTile.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + offset);
                currentTile.name = tile.name;
                offset -= 175;
            }
        }

        // Menu tiles sprites
        for (int i = 0; i < menuTiles.Count; i++) { SetMenuSprite(i); }

        // Editor menu default values
        UI.I.editor.nextLevelField.text = LevelManager.I.currentLevel.nextLevel;
        UI.I.editor.remixLevelField.text = LevelManager.I.currentLevel.remixLevel;
        UI.I.editor.freeroamToggle.isOn = LevelManager.I.currentLevel.freeroam;

        // Debug import level
        if (GameManager.I.IsDebug()) UI.I.editor.import.SetActive(true);
    }

    void OnDisable() { I = null; }

    // Set the preview image (right now, creates a tile every frame for rendering sprites, ow...)
    void Update()
    {
        // Coordinates
        coords.text = $"({LevelManager.I.worldOffsetX / 14},{LevelManager.I.worldOffsetY / 8})";

        // Preview sprite (on tilemap)
        if (isPlacing)
        {
            if (tileToPlace == ObjectTypes.Arrow) spriteRenderer.sprite = colorArrowSprite;
            else if (tileToPlace == ObjectTypes.NegativeArrow) spriteRenderer.sprite = badArrowSprite;
            else spriteRenderer.sprite = LevelManager.I.CreateTile(tileToPlace.ToString(), new(), Vector3Int.zero).tileSprite;
        } else spriteRenderer.sprite = deletionSprite;

        // Move mouse selector (on tilemap)
        if (UI.I.editor.self.activeSelf || tileList.activeSelf) return;
        Vector3Int mousePos = GetMousePositionOnGrid();
        if (mousePos == Vector3.back) return;

        mousePos -= new Vector3Int(LevelManager.I.worldOffsetX, LevelManager.I.worldOffsetY);
        spriteRenderer.transform.position = editorTilemap.GetCellCenterWorld(mousePos);
    }

    // Returns the mouse position on the playable grid
    internal Vector3Int GetMousePositionOnGrid()
    {
        if (LevelManager.I.currentLevel == null) return Vector3Int.back;
        
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = LevelManager.I.tilemapCollideable.WorldToCell(worldPoint);

        if (!LevelManager.I.CheckSceneInbounds(gridPos)) return Vector3Int.back;
        return gridPos;
    }

    // Places multiple tiles
    internal IEnumerator MultiPlace()
    {
        while (true)
        {
            if (UI.I.editor.self.activeSelf || tileList.activeSelf) yield break;

            if (popup.activeSelf) { multiClick = null; yield break; }

            // Checks mouse position
            Vector3Int gridPos = GetMousePositionOnGrid();
            if (gridPos != Vector3.back)
            {
                // Places the tile
                if (isPlacing) AddCommand(new EditorPlace(tileToPlace, gridPos));
                else AddCommand(new EditorDelete(gridPos));
            }

            // Waits and does another loop
            yield return new WaitForSeconds(0.005f);
        }
    }

    // Returns a tile from the level tilemap
    public GameTile GetEditorTile(Vector3Int position)
    {
        GameTile tile = LevelManager.I.tilemapObjects.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.I.tilemapCollideable.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.I.tilemapWinAreas.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.I.tilemapHazards.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.I.tilemapEffects.GetTile<GameTile>(position);
        if (!tile) tile = LevelManager.I.tilemapCustoms.GetTile<GameTile>(position);
        return tile;
    }

    // Updates the selected tile's custom text
    public void UpdateCustomText(string text)
    {
        if (!editingTile) return;

        // stupid ren was here
        if (!LevelManager.I.typesCustomsList.Contains(editingTile.GetTileType())) return;

        // Get the real tile that you can edit
        var existingRule = LevelManager.I.customTileInfo.Find(rule => { return rule.position == editingTile.position; });
        if (existingRule != null) existingRule.text = text;
        else LevelManager.I.customTileInfo.Add(new(editingTile.position, text));
        UI.I.global.SendMessage($"Set custom text to \"{text}\".", 2.25f);
        customInputField.interactable = false;
    }

    // Updates the selected tile's pushable
    public void UpdatePushable(bool value)
    {
        if (!editingTile || !editingTile.directions.editorPushable || ignoreUpdateEvent) return;
        editingTile.directions.pushable = value;
        editingTile.directions.UpdateSprites();
        LevelManager.I.RefreshObjectTile(editingTile);
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
        if (editingTile.GetTileType() == ObjectTypes.Arrow || editingTile.GetTileType() == ObjectTypes.NegativeArrow) LevelManager.I.RefreshEffectTile(editingTile);
        else LevelManager.I.RefreshObjectTile(editingTile);
    }

    // Turn on/off a toggle without evoking an event
    private void ToggleToggle(Toggle toggle)
    {
        ignoreUpdateEvent = true;
        toggle.isOn = !toggle.isOn;
        ignoreUpdateEvent = false;
    }

    // Sets one of the menu sprites
    private void SetMenuSprite(int index)
    {
        GameTile tile = LevelManager.I.CreateTile(GameManager.save.preferences.editorTiles[index].ToString(), new(), Vector3Int.zero);
        if (tile.GetTileType() == ObjectTypes.Arrow) menuTiles[index].sprite = colorArrowSprite;
        else if (tile.GetTileType() == ObjectTypes.NegativeArrow) menuTiles[index].sprite = badArrowSprite;
        else menuTiles[index].sprite = tile.tileSprite;
    }

    // UI tile list event
    public void SelectListTile(GameTile tile, bool toggleMenu = true)
    {
        GameManager.save.preferences.editorTiles[selectedTileIndex] = tile.GetTileType();
        SelectMenuTile(selectedTileIndex);
        if (toggleMenu) ToggleTileMenu();
    }

    // Selects a menu tile event
    public void SelectMenuTile(int index)
    {
        selectedTileIndex = index;
        tileToPlace = GameManager.save.preferences.editorTiles[selectedTileIndex];
        menuSelectors.ForEach(sel => sel.SetActive(false));
        menuSelectors[index].SetActive(true);
        SetMenuSprite(selectedTileIndex);
    }

    // Toggles the tile list on/off
    public void ToggleTileMenu()
    {
        if (UI.I.editor.self.activeSelf) return;
        if (popup.activeSelf) { popup.SetActive(false); return; }

        if (tileList.activeSelf) tileList.SetActive(false);
        else tileList.SetActive(true);
    }

    // Undoing
    internal bool IsUndoQueueValid() { return commandHistory.Count > 0; }

    internal void Undo()
    {
        commandHistory[^1].Undo();
        commandHistory.RemoveAt(commandHistory.Count - 1);
        
        AudioManager.I.PlaySFX(AudioManager.undo, 1f, true);
    }

    // Execute a command, and add to history    
    private void AddCommand(ICommand command)
    {
        bool result = command.Execute();
        if (result) commandHistory.Add(command);
    }
}
