using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using static Serializables;
using static GameTile;
using static TransitionManager.Transitions;

public class LevelManager : MonoBehaviour
{
    // Tile References & Others //
    internal readonly ObjectTypes[] typesSolidsList = { ObjectTypes.Wall, ObjectTypes.AntiWall };
    internal readonly ObjectTypes[] typesObjectList = { ObjectTypes.Box, ObjectTypes.Circle, ObjectTypes.Hexagon, ObjectTypes.Mimic };
    internal readonly ObjectTypes[] typesAreas = { ObjectTypes.Area, ObjectTypes.InverseArea, ObjectTypes.OutboundArea };
    internal readonly ObjectTypes[] typesHazardsList = { ObjectTypes.Hazard, ObjectTypes.Void };
    internal readonly ObjectTypes[] typesEffectsList = { ObjectTypes.Invert, ObjectTypes.Arrow, ObjectTypes.NegativeArrow, ObjectTypes.Orb };
    internal readonly ObjectTypes[] typesCustomsList = { ObjectTypes.Level, ObjectTypes.Fake, ObjectTypes.NPC };
    internal readonly ObjectTypes[] customSpriters = { ObjectTypes.NPC, ObjectTypes.Fake };
    internal readonly ObjectTypes[] customMovers = { ObjectTypes.Hexagon, ObjectTypes.Mimic };
    [HideInInspector] public static LevelManager Instance;
    [HideInInspector] public GameTile wallTile;
    [HideInInspector] public GameTile antiwallTile;
    [HideInInspector] public GameTile boxTile;
    [HideInInspector] public GameTile circleTile;
    [HideInInspector] public GameTile hexagonTile;
    [HideInInspector] public GameTile mimicTile;
    [HideInInspector] public GameTile areaTile;
    [HideInInspector] public GameTile inverseAreaTile;
    [HideInInspector] public GameTile outboundAreaTile;
    [HideInInspector] public GameTile levelTile;
    [HideInInspector] public GameTile fakeTile;
    [HideInInspector] public GameTile npcTile;
    [HideInInspector] public GameTile hazardTile;
    [HideInInspector] public GameTile voidTile;
    [HideInInspector] public GameTile invertTile;
    [HideInInspector] public GameTile arrowTile;
    [HideInInspector] public GameTile negativeArrowTile;
    [HideInInspector] public GameTile orbTile;

    // Grids and tilemaps //
    private Grid levelGrid;
    private Grid extraGrid;
    [HideInInspector] public Tilemap tilemapCollideable;
    [HideInInspector] public Tilemap tilemapObjects;
    [HideInInspector] public Tilemap tilemapWinAreas;
    [HideInInspector] public Tilemap tilemapHazards;
    [HideInInspector] public Tilemap tilemapEffects;
    [HideInInspector] public Tilemap tilemapCustoms;
    [HideInInspector] public Tilemap tilemapLetterbox;
    [HideInInspector] public Tilemap extrasOutlines;
    private TilemapRenderer areaRenderer;
    internal Vector3 originalPosition;
    internal int worldOffsetX = 0;
    internal int worldOffsetY = 0;

    // Level data //
    [HideInInspector] public SerializableLevel currentLevel = null;
    [HideInInspector] public string currentLevelID = null;
    [HideInInspector] public string levelEditorName = null;
    internal List<ObjectTypes> formQueue = new(capacity: 101); // 100 + 1 form capacity (for now)
    internal List<SerializableCustomInfo> customTileInfo = new();
    private readonly List<GameTile> levelSolids = new();
    private readonly List<GameTile> levelObjects = new();
    private readonly List<GameTile> levelWinAreas = new();
    private readonly List<GameTile> levelHazards = new();
    private readonly List<GameTile> levelEffects = new();
    private readonly List<GameTile> levelCustoms = new();
    private readonly List<GameTile> movementBlacklist = new();
    private readonly List<HexagonTile> lateMove = new();
    private readonly List<GameTile> toDestroy = new();
    private readonly List<Tiles> undoSequence = new(capacity: 100); // 100 undo capacity (for now)
    private readonly int boundsX = 13;
    private readonly int boundsY = -7;
    private int defaultOverlapLayer;
    private Checker background;

    // Player //
    private Coroutine timerCoroutine = null;
    private bool doPushSFX = false;
    private float levelTimer = 0f;
    private int levelMoves = 0;
    private bool noMove = false;
    public bool isPaused = false;
    public bool hasWon;

    void Awake()
    {
        // Singleton (LevelManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Getting grids and tilemap references
        SceneManager.sceneLoaded += RefreshGameOnSceneLoad;
        TryGetSceneReferences();

        // Getting tile references
        wallTile = Resources.Load<WallTile>("Tiles/Solids/Wall");
        antiwallTile = Resources.Load<AntiWallTile>("Tiles/Solids/Anti Wall");
        boxTile = Resources.Load<BoxTile>("Tiles/Objects/Box");
        circleTile = Resources.Load<CircleTile>("Tiles/Objects/Circle");
        hexagonTile = Resources.Load<HexagonTile>("Tiles/Objects/Hexagon");
        mimicTile = Resources.Load<MimicTile>("Tiles/Objects/Mimic");
        areaTile = Resources.Load<WinAreaTile>("Tiles/Areas/Area");
        inverseAreaTile = Resources.Load<InverseWinAreaTile>("Tiles/Areas/Inverse Area");
        outboundAreaTile = Resources.Load<OutboundAreaTile>("Tiles/Areas/Outbound Area");
        hazardTile = Resources.Load<HazardTile>("Tiles/Hazards/Hazard");
        voidTile = Resources.Load<VoidTile>("Tiles/Hazards/Void");
        invertTile = Resources.Load<InvertTile>("Tiles/Effects/Invert");
        arrowTile = Resources.Load<ArrowTile>("Tiles/Effects/Arrow");
        negativeArrowTile = Resources.Load<NegativeArrowTile>("Tiles/Effects/Negative Arrow");
        orbTile = Resources.Load<OrbTile>("Tiles/Effects/Orb");
        levelTile = Resources.Load<LevelTile>("Tiles/Customs/Level");
        fakeTile = Resources.Load<FakeTile>("Tiles/Customs/Fake");
        npcTile = Resources.Load<NPCTile>("Tiles/Customs/NPC");

        // Defaults
        defaultOverlapLayer = areaRenderer.sortingOrder;
        hasWon = false;

        // Editor (with file persistence per session)
        levelEditorName = "EditorSession";
        currentLevelID = null;
        currentLevel = null;
    }

    // Gets the scene references for later use (should be called every time on scene change (actually no i lied))
    private void TryGetSceneReferences()
    {
        // Level grids and tilemaps
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject != null ? gridObject.GetComponent<Grid>() : null;
        tilemapCollideable = gridObject != null ? gridObject.Find("Collideable").GetComponent<Tilemap>() : null;
        tilemapObjects = gridObject != null ? gridObject.Find("Objects").GetComponent<Tilemap>() : null;
        tilemapWinAreas = gridObject != null ? gridObject.Find("Overlaps").GetComponent<Tilemap>() : null;
        tilemapHazards = gridObject != null ? gridObject.Find("Hazards").GetComponent<Tilemap>() : null;
        tilemapEffects = gridObject != null ? gridObject.Find("Effects").GetComponent<Tilemap>() : null;
        tilemapCustoms = gridObject != null ? gridObject.Find("Customs").GetComponent<Tilemap>() : null;
        tilemapLetterbox = gridObject != null ? gridObject.Find("Letterbox").GetComponent<Tilemap>() : null;

        // Extra grids and tilemaps
        Transform extraObject = transform.Find("Extras");
        extraGrid = extraObject != null ? extraObject.GetComponent<Grid>() : null;
        extrasOutlines = extraObject != null ? extraObject.Find("Outlines").GetComponent<Tilemap>() : null;

        areaRenderer = tilemapWinAreas.GetComponent<TilemapRenderer>();
        originalPosition = new Vector3(tilemapObjects.transform.position.x, tilemapObjects.transform.position.y, tilemapObjects.transform.position.z);
    }

    // Adds a tile to the private objects list
    public void AddToObjectList(GameTile tile)
    {
        if (!typesObjectList.Contains(tile.GetTileType())) return;
        else if (!levelObjects.Contains(tile)) levelObjects.Add(tile);
    }

    // Adds a tile to the private win areas list
    public void AddToWinAreasList(GameTile tile)
    {
        if (!typesAreas.Contains(tile.GetTileType())) return;
        else if (!levelWinAreas.Contains(tile)) levelWinAreas.Add(tile);
    }

    // Adds a tile to the private collideable list
    public void AddToCollideableList(GameTile tile)
    {
        if (!typesSolidsList.Contains(tile.GetTileType())) return;
        else if (!levelSolids.Contains(tile)) levelSolids.Add(tile);
    }

    // Adds a tile to the private hazards list
    public void AddToHazardsList(GameTile tile)
    {
        if (!typesHazardsList.Contains(tile.GetTileType())) return;
        else if (!levelHazards.Contains(tile)) levelHazards.Add(tile);
    }

    // Adds a tile to the private effects list
    public void AddToEffectsList(GameTile tile)
    {
        if (!typesEffectsList.Contains(tile.GetTileType())) return;
        else if (!levelEffects.Contains(tile)) levelEffects.Add(tile);
    }

    // Adds a tile to the private customs list
    public void AddToCustomsList(GameTile tile)
    {
        if (!typesCustomsList.Contains(tile.GetTileType())) return;
        else if (!levelCustoms.Contains(tile)) levelCustoms.Add(tile);
    }

    // Adds a tile to the private to destroy queue (hazards use this)
    public void AddToDestroyQueue(GameTile tile)
    {
        if (!toDestroy.Contains(tile)) toDestroy.Add(tile);
    }

    // Late moving a tile
    public void AddToLateMove(HexagonTile tile)
    {
        if (!lateMove.Contains(tile)) lateMove.Add(tile);
    }

    // Saves a level to the game's persistent path
    public void SaveLevel(string levelName, string levelID = default, bool silent = true)
    {
        if (IsStringEmptyOrNull(levelName)) return;
        levelName = levelName.Trim();

        // Level id stuff
        if (levelID == default) levelID = $"{Random.Range(1000000, 1000000000)}";

        // Create the level object
        SerializableLevel level = new() { levelName = levelName };

        // Populate the level
        levelSolids.ForEach(tile => level.tiles.solidTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelObjects.ForEach(tile => level.tiles.objectTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelWinAreas.ForEach(tile => level.tiles.overlapTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelHazards.ForEach(tile => level.tiles.hazardTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelEffects.ForEach(tile => level.tiles.effectTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelCustoms.ForEach(tile => level.tiles.customTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));

        // Freeroam & hideui
        if (currentLevel != null) {
            level.freeroam = currentLevel.freeroam;
            level.hideUI = currentLevel.hideUI;
        }

        // Add custom tile information
        foreach (var tile in customTileInfo)
        {
            if (!tilemapCustoms.GetTile<CustomTile>(tile.position) || level.tiles.customTileInfo.Any(pos => { return pos.position == tile.position; })) continue;
            level.tiles.customTileInfo.Add(tile);
        }

        // Save the level locally
        string levelPath = $"{Application.persistentDataPath}/Custom Levels/{levelID}.level";
        File.WriteAllText(levelPath, JsonUtility.ToJson(level, false));
        if (!silent) UI.Instance.global.SendMessage($"Saved level \"{levelName}\" with ID \"{levelID}\".", 4.0f);
    }

    // Load and build a level
    public bool LoadLevel(string levelID, bool external = false, bool silent = true)
    {
        if (IsStringEmptyOrNull(levelID)) return false;
        levelID = levelID.Trim();

        // Gets the new level
        SerializableLevel checkLevel = GetLevel(levelID, external, silent);
        if (checkLevel == null) return false;
        else currentLevel = checkLevel;

        // Clears the current level
        MoveTilemaps(originalPosition, true);
        ClearLevel();

        // Loads the level
        currentLevelID = levelID;
        BuildLevel(currentLevel.tiles);

        // Start the level timer (coro) and reset moves
        levelTimer = 0;
        levelMoves = 0;
        timerCoroutine = StartCoroutine(LevelTimer());
        UI.Instance.ingame.SetLevelMoves(levelMoves);

        // Swapping mechanic startup
        var playables = InputManager.Instance.GetPlayableObjects();
        if (playables.Count == 1) { formQueue.Add(playables[0].GetTileType()); }
        else formQueue.Add(ObjectTypes.Mimic); // should never happen anyways.

        // Yay! UI!
        if (!silent) UI.Instance.global.SendMessage($"Loaded level \"{currentLevel.levelName}\"");

        // Hide UI?
        if (currentLevel.hideUI) {
            tilemapLetterbox.gameObject.SetActive(true);
            UI.Instance.ingame.Toggle(false);
            return true;
        }

        // UI Stuff
        GameData.Level levelAsSave = GameManager.save.game.levels.Find(l => l.levelID == levelID);
        UI.Instance.ingame.SetAreaCount(0, levelWinAreas.Count(area => { return area.GetTileType() == ObjectTypes.Area; }));
        UI.Instance.ingame.SetLevelName(currentLevel.levelName);
        if (levelAsSave != null) {
            UI.Instance.pause.SetBestTime(levelAsSave.stats.bestTime);
            UI.Instance.pause.SetBestMoves(levelAsSave.stats.totalMoves);
        } else {
            UI.Instance.pause.SetBestTime(0f);
            UI.Instance.pause.SetBestMoves(0);
        }

        return true;
    }

    // Load and build a level
    public void ReloadLevel(bool silent = true)
    {
        if (currentLevel == null) return;

        // Clears the current level
        ClearLevel();

        // Restart timer and level stats
        levelTimer = 0f;
        levelMoves = 0;
        UI.Instance.ingame.SetLevelMoves(levelMoves);
        timerCoroutine = StartCoroutine(LevelTimer());

        // Soft "loads" the new level (doesnt use LoadLevel)
        if (!silent) UI.Instance.global.SendMessage("Reloaded level.");
        currentLevel = GetLevel(currentLevelID, true);
        BuildLevel(currentLevel.tiles);

        // UI!
        UI.Instance.ingame.SetAreaCount(0, levelWinAreas.Count(area => { return area.GetTileType() == ObjectTypes.Area; }));
    }

    // Builds the level
    private void BuildLevel(Tiles level)
    {
        if (level == null) return;

        // Allow orb spawning in the level?
        if (GameManager.save.game.collectedOrbs.Contains(currentLevelID))
        {
            level.effectTiles = level.effectTiles.FindAll(tile => { return tile.type != "Orb"; });
        }

        // Build the level
        level.solidTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position)));
        level.objectTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position)));
        level.overlapTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position)));
        level.hazardTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position)));
        level.effectTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position)));
        level.customTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position)));
        
        // Apply all custom tile text
        level.customTileInfo.ForEach(tile => customTileInfo.Add(new(tile.position, tile.text)));
        foreach (var tile in level.customTileInfo)
        {
            CustomTile realTile = tilemapCustoms.GetTile<CustomTile>(tile.position);
            if (realTile) { realTile.customText = tile.text; SetCustomSprite(realTile, false); RefreshCustomTile(realTile); }
            else customTileInfo.Remove(tile);
        }
    }

    // Clears the current level
    public void ClearLevel(bool soft = false)
    {
        levelGrid.GetComponentsInChildren<Tilemap>().ToList().ForEach(layer => { if(layer.name != "Letterbox" && layer.name != "Scanlines") layer.ClearAllTiles(); });
        if (!soft) {
            if (timerCoroutine != null) { StopCoroutine(timerCoroutine); }
            InputManager.Instance.latestMovement = Vector3Int.back;
            ClearUndoFrames();
        }

        InputManager.Instance.latestTile = ObjectTypes.Hexagon;
        movementBlacklist.Clear();
        customTileInfo.Clear();
        levelSolids.Clear();
        levelObjects.Clear();
        levelWinAreas.Clear();
        levelHazards.Clear();
        levelEffects.Clear();
        levelCustoms.Clear();
    }

    // Moves a tile (needs optimizing)
    public bool TryMove(Vector3Int startingPosition, Vector3Int newPosition, Vector3Int direction, bool removeFromQueue = false, bool beingPushed = false)
    {
        if (noMove) return false;

        // Check if the tile exists
        GameTile tile = tilemapObjects.GetTile<GameTile>(startingPosition);
        if (!tile) return false;

        // Is the tile pushable?
        if (!tile.directions.pushable && beingPushed) return false;

        // Disallows MOVING a tile that has already moved
        if (movementBlacklist.Contains(tile) && !beingPushed) return false;

        // Scene bounds (x,y always at 0)
        if (!CheckSceneInbounds(newPosition) && !customMovers.Contains(tile.GetTileType())) return false;

        // Checks if directions are null
        if ((direction.y > 0 && !tile.directions.up ||
            direction.y < 0 && !tile.directions.down ||
            direction.x < 0 && !tile.directions.left ||
            direction.x > 0 && !tile.directions.right)
            && !beingPushed) {
            if (removeFromQueue) movementBlacklist.Add(tile);
            return false;
        }

        // Moves the tile if all collision checks pass
        newPosition = tile.CollisionHandler(newPosition, direction, tilemapObjects, tilemapCollideable, beingPushed);
        if (newPosition == Vector3.back || newPosition == startingPosition || (movementBlacklist.Contains(tile) && !beingPushed) || noMove) return false; // also re-checking for blacklist
        MoveTile(startingPosition, newPosition, tile);

        // Updates new current position of the tile
        if (beingPushed) doPushSFX = true;
        tile.position = newPosition;

        // Change "scene" if on world map?
        if (currentLevel.freeroam)
        {
            // X POSITION: -14 / +14.
            // Y POSITION: -8 / +8.
            if (tile.position.x < 0 + worldOffsetX) { MoveTilemaps(new Vector3(14, 0)); worldOffsetX -= 14; }
            else if (tile.position.x > boundsX + worldOffsetX) { MoveTilemaps(new Vector3(-14, 0)); worldOffsetX += 14; }
            else if (tile.position.y > 0 + worldOffsetY) { MoveTilemaps(new Vector3(0, -8)); worldOffsetY += 8; }
            else if (tile.position.y < boundsY + worldOffsetY) { MoveTilemaps(new Vector3(0, 8)); worldOffsetY -= 8; }
        }

        // Removes from movement queue
        if (removeFromQueue) { if (!movementBlacklist.Contains(tile)) movementBlacklist.Add(tile); }

        // Marks all the objects that should be deleted
        GameTile hazard = tilemapHazards.GetTile<GameTile>(tile.position);
        if (hazard)
        {
            AddToDestroyQueue(tile);
            if (hazard.GetTileType() == ObjectTypes.Void) RemoveTile(hazard);
        }

        // Tile effect?
        EffectTile effect = tilemapEffects.GetTile<EffectTile>(tile.position);
        CustomTile custom = tilemapCustoms.GetTile<CustomTile>(tile.position);
        if (effect) effect.Effect(tile);
        if (custom) custom.Effect(tile);

        return true;
    }

    // Moves a tile, no other cases
    public void MoveTile(Vector3Int startingPos, Vector3Int newPos, GameTile tile)
    {
        // Sets the new tile and removes the old one
        tilemapObjects.SetTile(newPos, tile);
        tilemapObjects.SetTile(startingPos, null);
    }

    // Places a tile using its own position
    public void PlaceTile(GameTile tile)
    {
        switch (tile.GetTileType())
        {
            case ObjectTypes t when typesSolidsList.Contains(t):
                tilemapCollideable.SetTile(tile.position, tile);
                levelSolids.Add(tile);
                break;

            case ObjectTypes t when typesAreas.Contains(t):
                tilemapWinAreas.SetTile(tile.position, tile);
                levelWinAreas.Add(tile);
                break;

            case ObjectTypes t when typesHazardsList.Contains(t):
                tilemapHazards.SetTile(tile.position, tile);
                levelHazards.Add(tile);
                break;

            case ObjectTypes t when typesEffectsList.Contains(t):
                tilemapEffects.SetTile(tile.position, tile);
                levelEffects.Add(tile);
                break;

            case ObjectTypes t when typesCustomsList.Contains(t):
                tilemapCustoms.SetTile(tile.position, tile);
                levelCustoms.Add(tile);
                break;

            default:
                tilemapObjects.SetTile(tile.position, tile);
                levelObjects.Add(tile);
                break;
        }
    }

    // Removes a tile from a tilemap
    public void RemoveTile(GameTile tile)
    {
        switch (tile.GetTileType())
        {
            case ObjectTypes t when typesSolidsList.Contains(t):
                tilemapCollideable.SetTile(tile.position, null);
                levelSolids.Remove(tile);
                break;

            case ObjectTypes t when typesAreas.Contains(t):
                tilemapWinAreas.SetTile(tile.position, null);
                levelWinAreas.Remove(tile);
                break;

            case ObjectTypes t when typesHazardsList.Contains(t):
                tilemapHazards.SetTile(tile.position, null);
                levelHazards.Remove(tile);
                break;

            case ObjectTypes t when typesEffectsList.Contains(t):
                tilemapEffects.SetTile(tile.position, null);
                levelEffects.Remove(tile);
                break;

            case ObjectTypes t when typesCustomsList.Contains(t):
                tilemapCustoms.SetTile(tile.position, null);
                levelCustoms.Remove(tile);
                break;

            default:
                tilemapObjects.SetTile(tile.position, null);
                levelObjects.Remove(tile);
                break;
        }
    }

    // Creates a gametile
    public GameTile CreateTile(string type, Directions defaultDirections, Vector3Int defaultPosition)
    {
        // Instantiate correct tile
        GameTile tile = type switch
        {
            "Circle" => Instantiate(circleTile),
            "Hexagon" => Instantiate(hexagonTile),
            "Mimic" => Instantiate(mimicTile),
            "Wall" => Instantiate(wallTile),
            "AntiWall" => Instantiate(antiwallTile),
            "Area" => Instantiate(areaTile),
            "InverseArea" => Instantiate(inverseAreaTile),
            "OutboundArea" => Instantiate(outboundAreaTile),
            "Hazard" => Instantiate(hazardTile),
            "Void" => Instantiate(voidTile),
            "Invert" => Instantiate(invertTile),
            "Arrow" => Instantiate(arrowTile),
            "NegativeArrow" => Instantiate(negativeArrowTile),
            "Orb" => Instantiate(orbTile),
            "Level" => Instantiate(levelTile),
            "Fake" => Instantiate(fakeTile),
            "NPC" => Instantiate(npcTile),
            _ => Instantiate(boxTile) // Default, covers box types
        };

        // Apply tile defaults
        tile.directions = defaultDirections;
        tile.position = defaultPosition;
        tile.PrepareTile();
        return tile;
    }

    // Returns if a position is inside or outside the level bounds
    public bool CheckSceneInbounds(Vector3Int position, bool hexSpecial = false)
    {
        if (GameManager.Instance.IsEditor()) return !(position.x < 0 + worldOffsetX || position.x > boundsX + worldOffsetX || position.y > 0 + worldOffsetY || position.y < boundsY + worldOffsetY);
        if (currentLevel.freeroam && hexSpecial) return true;
        if (currentLevel.freeroam && currentLevel.hideUI) return true;
        return !(position.x < 0 + worldOffsetX || position.x > boundsX + worldOffsetX || position.y > 0 + worldOffsetY || position.y < boundsY + worldOffsetY);
    }

    // Applies gravity using a direction
    internal void ApplyGravity(Vector3Int movement)
    {
        // Clears blacklist
        movementBlacklist.Clear();
        toDestroy.Clear();
        lateMove.Clear();
        doPushSFX = false;
        noMove = false;

        // Sort by move "priority"
        List<GameTile> moveList = levelObjects.OrderBy(tile => tile.GetTileType() != ObjectTypes.Hexagon).ToList();
        List<bool> validation = new();

        // Moves every object
        foreach (var tile in moveList)
        {
            if (!movementBlacklist.Contains(tile))
            {
                // Tries to move a tile
                validation.Add(TryMove(tile.position, tile.position + movement, movement, true));
            }
        }

        // Late moves (stupid...)
        foreach (var tile in lateMove)
        {
            if (!movementBlacklist.Contains(tile))
            {
                // Tries to move a tile
                validation.Add(TryMove(tile.position, tile.position + movement, movement, true));
            }
        }

        // Tile pushed SFX
        if (doPushSFX) AudioManager.Instance.PlaySFX(AudioManager.tilePush, 0.50f);

        // Destroys all marked object tiles.
        if (toDestroy.Count > 0) AudioManager.Instance.PlaySFX(AudioManager.tileDeath, 0.40f);
        foreach (GameTile tile in toDestroy) { RemoveTile(tile); }

        // Win check, add one move to the player
        if (validation.Contains(true)) levelMoves++;
        else RemoveUndoFrame();
        if (UI.Instance) UI.Instance.ingame.SetLevelMoves(levelMoves);
        CheckCompletion();
    }

    // Checks if you've won
    private void CheckCompletion()
    {
        // Level win condition:
        // All area tiles have some object overlapping them and at least 1 exists,
        // no other areas are being overlapped.
        bool winCondition = 
            levelWinAreas.All(overlap =>
                {
                    if (!typesAreas.Contains(overlap.GetTileType())) return true;

                    GameTile objectOverlap = tilemapObjects.GetTile<GameTile>(overlap.position);
                    ObjectTypes type = overlap.GetTileType();

                    return (objectOverlap != null && type == ObjectTypes.Area) ||
                    (objectOverlap == null && type == ObjectTypes.InverseArea) ||
                    (objectOverlap == null && type == ObjectTypes.OutboundArea);
                }
            ) && levelWinAreas.Any(area => area.GetTileType() == ObjectTypes.Area); // At least one exists

        // Inverted win condition:
        // All area tiles have some object overlapping them and at least 1 exists,
        // all object tiles must be overlapping said inverted areas,
        // level MUST have a remix level defined (not null)
        bool remixCondition =
            levelWinAreas.All(overlap => // All inverse areas are overlapped
                {
                    if (!typesAreas.Contains(overlap.GetTileType())) return true;

                    GameTile objectOverlap = tilemapObjects.GetTile<GameTile>(overlap.position);
                    ObjectTypes type = overlap.GetTileType();

                    return (objectOverlap != null && type == ObjectTypes.InverseArea) ||
                    (objectOverlap == null && type == ObjectTypes.Area) ||
                    (objectOverlap == null && type == ObjectTypes.OutboundArea);
                }
            ) && levelWinAreas.Any(area => area.GetTileType() == ObjectTypes.InverseArea) // At least one exists
            && levelObjects.All(tile => // All level objects are overlapping inverse areas
                {
                    return tilemapWinAreas.GetTile<InverseWinAreaTile>(tile.position) != null;
                }
            ) && currentLevel.remixLevel != null;

        // Outbound win condition:
        // All outbound area tiles have some object overlapping them and at least 1 exists,
        // no other areas are being overlapped.
        bool outboundCondition = 
            levelWinAreas.All(overlap =>
                {
                    if (!typesAreas.Contains(overlap.GetTileType())) return true;

                    GameTile objectOverlap = tilemapObjects.GetTile<GameTile>(overlap.position);
                    ObjectTypes type = overlap.GetTileType();

                    return (objectOverlap != null && type == ObjectTypes.OutboundArea) ||
                    (objectOverlap == null && type == ObjectTypes.InverseArea) ||
                    (objectOverlap == null && type == ObjectTypes.Area);
                }
            ) && levelWinAreas.Any(area => area.GetTileType() == ObjectTypes.OutboundArea); // At least one exists

        // UI area count
        SetUIAreaCount();

        // Outbound win
        if (outboundCondition)
        {
            // Level + savedata
            GameData.LevelChanges changes = new(false, true, -1, -1);
            GameManager.Instance.UpdateSavedLevel(currentLevelID, changes, true);
            GameManager.save.game.hasSeenOutbound = true;

            // UI
            EventSystem.current.SetSelectedGameObject(UI.Instance.win.menuButton);
            UI.Instance.win.ToggleEditButton(GameManager.Instance.isEditing || GameManager.Instance.IsDebug());
            UI.Instance.win.SetTotalTime(changes.time);
            UI.Instance.win.SetTotalMoves(changes.moves);
            UI.Instance.win.Toggle(true);
            hasWon = true;
        }

        // Load remix level!
        if (remixCondition)
        {
            if (!GameManager.save.game.hasSeenRemix) GameManager.save.game.hasSeenRemix = true;
            TransitionManager.Instance.TransitionIn(Unknown, ActionRemixCondition, currentLevel.remixLevel);
            return;
        }

        // If won, do the thing
        if (winCondition)
        {
            // Level savedata
            GameData.LevelChanges changes = new(true, false, (float)Math.Round(levelTimer, 2), levelMoves);
            GameManager.Instance.UpdateSavedLevel(currentLevelID, changes, true);

            // UI
            EventSystem.current.SetSelectedGameObject(UI.Instance.win.menuButton);
            UI.Instance.win.ToggleEditButton(GameManager.Instance.isEditing || GameManager.Instance.IsDebug());
            UI.Instance.win.ToggleNextLevel(!IsStringEmptyOrNull(currentLevel.nextLevel));
            UI.Instance.win.SetTotalTime(changes.time);
            UI.Instance.win.SetTotalMoves(changes.moves);
            UI.Instance.win.Toggle(true);
            hasWon = true;
        }
    }

    // Returns if currently in editor
    public bool IsAllowedToPlay() { return !(GameManager.Instance.IsBadScene() || isPaused || hasWon || DialogManager.Instance.inDialog || TransitionManager.Instance.inTransition); }

    // Is string empty or null
    public bool IsStringEmptyOrNull(string str) { return str == null || str == string.Empty; }

    // Gets a level and returns it as a serialized object
    public SerializableLevel GetLevel(string levelID, bool external, bool silent = false)
    {
        string externalPath = $"{Application.persistentDataPath}/Custom Levels/{levelID}.level";
        string level = null;

        // Internal/external level import.
        if (external && File.Exists(externalPath)) level = File.ReadAllText(externalPath);
        else {
            TextAsset internalCheck = Resources.Load<TextAsset>($"Levels/{levelID}");
            if (internalCheck) level = internalCheck.text;
        }

        // Invalid level!
        if (level == null) { if (!silent) UI.Instance.global.SendMessage($"Invalid level! ({levelID})", 2.5f); return null; }

        return JsonUtility.FromJson<SerializableLevel>(level);
    }

    // Pauses or resumes the game.
    public void PauseResumeGame(bool status)
    {
        if (status) {
            EventSystem.current.SetSelectedGameObject(UI.Instance.pause.backToMenu);
            UI.Instance.pause.ToggleEditButton(GameManager.Instance.isEditing || GameManager.Instance.IsDebug());
        }

        UI.Instance.pause.Toggle(status);
        isPaused = status;
    }

    // Refreshes an object tile
    public void RefreshObjectTile(GameTile tile)
    {
        tilemapObjects.SetTile(tile.position, null);
        tilemapObjects.SetTile(tile.position, tile);
    }

    // Refreshes an effect tile
    public void RefreshEffectTile(GameTile tile)
    {
        tilemapEffects.SetTile(tile.position, null);
        tilemapEffects.SetTile(tile.position, tile);
    }

    // Refreshes an area tile
    public void RefreshAreaTile(GameTile tile)
    {
        tilemapWinAreas.SetTile(tile.position, null);
        tilemapWinAreas.SetTile(tile.position, tile);
    }

    // Refreshes a custom tile
    public void RefreshCustomTile(CustomTile tile)
    {
        tilemapCustoms.SetTile(tile.position, null);
        tilemapCustoms.SetTile(tile.position, tile);
    }

    // Refreshes the game and closes all UI's
    public void RefreshGameVars()
    {
        isPaused = false;
        hasWon = false;
        levelTimer = 0f;
        levelMoves = 0;
        worldOffsetX = 0;
        worldOffsetY = 0;

        DialogManager.Instance.loadedDial = null;
        tilemapLetterbox.gameObject.SetActive(true);
        extrasOutlines.gameObject.SetActive(true);
        UI.Instance.ingame.SetLevelTimer(levelTimer);
        UI.Instance.ingame.SetLevelMoves(levelMoves);
        MoveTilemaps(originalPosition, true);
    }

    // Sets all UI's to its defaults
    public void RefreshGameUI()
    {
        UI.Instance.ingame.Toggle(true);
        UI.Instance.pause.Toggle(false);
        UI.Instance.win.Toggle(false);
        UI.Instance.editor.Toggle(false);
    }

    // Gets called whenever you change scenes
    private void RefreshGameOnSceneLoad(Scene scene, LoadSceneMode sceneMode)
    {
        if (UI.Instance.selectors)
        {
            UI.Instance.selectors.right.SetParent(UI.Instance.selectors.gameObject.transform);
            UI.Instance.selectors.left.SetParent(UI.Instance.selectors.gameObject.transform);
            UI.Instance.selectors.sceneLoaded = true;
        }

        if (GameManager.Instance.noGameplayScenes.Contains(scene.name))
        {
            tilemapLetterbox.gameObject.SetActive(false);
            extrasOutlines.gameObject.SetActive(false);
            UI.Instance.ingame.Toggle(false);
            return;
        }

        // Get game background reference
        if (SceneManager.GetActiveScene().name == "Game" && !background) background = GameObject.Find("Static Background").GetComponent<Checker>();
    }

    // Level timer speedrun any%
    private IEnumerator LevelTimer()
    {
        while (!hasWon)
        {
            levelTimer += Time.deltaTime;
            if (UI.Instance) UI.Instance.ingame.SetLevelTimer(levelTimer);
            yield return null;
        }
    }

    // (World Scene) Moves all* tilemaps towards a direction
    internal void MoveTilemaps(Vector3 direction, bool force = false)
    {
        foreach (Transform tilemap in levelGrid.transform)
        {
            if (tilemap.name == "Letterbox") continue; // * except letterbox

            // Updates position based on direction
            if (force) tilemap.position = direction;
            else tilemap.position += direction;
        }
    }

    // Pings all areas
    internal void PingAllAreas(bool status)
    {
        if (status) areaRenderer.sortingOrder = 5;
        else areaRenderer.sortingOrder = defaultOverlapLayer;

        // foreach (GameTile area in levelWinAreas)
        // {
        //     if (status) tilemapWinAreas.GetTile<AreaTile>(area.position).Ping();
        //     else {
        //         GameManager.Instance.drawOver.sprite = null;
        //         RefreshAreaTile(area);
        //     }
        // }
    }

    // Adds an undo frame to the sequence (lord help me)
    internal void AddUndoFrame()
    {
        if (undoSequence.Count >= undoSequence.Capacity) RemoveUndoFrame(true);
        formQueue.Add(InputManager.Instance.latestTile);
        undoSequence.Add(new Tiles(levelSolids, levelObjects, levelWinAreas, levelHazards, levelEffects, levelCustoms, customTileInfo));
    }

    // Removes the latest undo frame from the sequence
    internal void RemoveUndoFrame(bool earliest = false)
    {
        if (undoSequence.Count <= 0) return;
        if (earliest) { undoSequence.RemoveAt(0); formQueue.RemoveAt(0); }
        else { undoSequence.RemoveAt(undoSequence.Count - 1); formQueue.RemoveAt(formQueue.Count - 1); }
    }

    // Clears all frames
    internal void ClearUndoFrames() { undoSequence.Clear(); formQueue.Clear(); }

    // Undo check
    internal bool IsUndoQueueValid() { return undoSequence.Count > 0; }

    // Undoes a move
    internal void Undo()
    {
        // Reload level snapshot (not very efficient)
        ClearLevel(true);
        BuildLevel(undoSequence[^1]);
        InputManager.Instance.latestTile = formQueue[^1];
        SetUIAreaCount();
        
        // Remove a move
        levelMoves--;
        if (UI.Instance) UI.Instance.ingame.SetLevelMoves(levelMoves);
    }

    // No more moving!
    internal void StopMovements()
    {
        // Gotta do it this way instead of clearing the movement list
        // to avoid changing the stack size!
        noMove = true;
        
        // foreach (GameTile tile in levelObjects)
        // {
        //     if (!movementBlacklist.Contains(tile)) movementBlacklist.Add(tile);
        // }
    }

    // Updates a custom tile's sprite
    internal void SetCustomSprite(CustomTile tile, bool refresh = true)
    {
        if (!customSpriters.Contains(tile.GetTileType())) return;

        if (tile.customText != string.Empty)
        {
            string stringCheck;

            // Checks if there is a sprite
            switch (tile.GetTileType())
            {
                case ObjectTypes.NPC:
                    if (tile.customText.Split(";").Length < 2) return;
                    stringCheck = (string)tile.customText.Split(";").GetValue(1);
                    break;
                
                case ObjectTypes.Fake:
                    stringCheck = tile.customText;
                    break;
                default:
                    return;
            }

            // Checks if the sprite exists
            Sprite spriteCheck = Resources.Load<Sprite>($"Sprites/Tiles/{stringCheck}");
            if (spriteCheck == null) return;

            // Sets the sprite and (optionally) refreshes the tile
            tile.tileSprite = spriteCheck;
            if (refresh) RefreshCustomTile(tile);
        }
    }

    // Returns the list of object tiles
    internal List<GameTile> GetObjectTiles()
    {
        return levelObjects;
    }

    // Returns the list of custom tiles
    internal List<GameTile> GetCustomTiles()
    {
        return levelCustoms;
    }

    // Finds all overlapped areas and sets the amount on the UI
    private void SetUIAreaCount()
    {
        UI.Instance.ingame.SetAreaCount(
            levelWinAreas.Count(area => { return area.GetTileType() == ObjectTypes.Area && tilemapObjects.GetTile<GameTile>(area.position) != null; }),
            levelWinAreas.Count(area => { return area.GetTileType() == ObjectTypes.Area; })
        );
    }

    // Actions //
    public void ActionRemixCondition(string remixID)
    {
        LoadLevel(remixID);
        TransitionManager.Instance.TransitionOut<string>(Unknown);
    }
}
