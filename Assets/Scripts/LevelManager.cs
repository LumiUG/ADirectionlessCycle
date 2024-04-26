using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static Serializables;
using static GameTile;

public class LevelManager : MonoBehaviour
{
    // Basic //
    private readonly ObjectTypes[] typesSolidsList = { ObjectTypes.Wall };
    private readonly ObjectTypes[] typesObjectList = { ObjectTypes.Box, ObjectTypes.Circle, ObjectTypes.Hexagon };
    private readonly ObjectTypes[] typesOverlapsList = { ObjectTypes.Area, ObjectTypes.Hazard };
    [HideInInspector] public static LevelManager Instance;
    [HideInInspector] public GameTile wallTile;
    [HideInInspector] public GameTile boxTile;
    [HideInInspector] public GameTile hexagonTile;
    [HideInInspector] public GameTile circleTile;
    [HideInInspector] public GameTile areaTile;
    [HideInInspector] public GameTile hazardTile;
    public int boundsX = 19;
    public int boundsY = -11;

    // Grids and tilemaps //
    private Grid levelGrid;
    [HideInInspector] public Tilemap tilemapCollideable;
    [HideInInspector] public Tilemap tilemapObjects;
    [HideInInspector] public Tilemap tilemapOverlaps;

    // Level data //
    private readonly List<GameTile> levelSolids = new();
    private readonly List<GameTile> levelObjects = new();
    private readonly List<GameTile> levelOverlaps = new();
    private readonly List<GameTile> movementBlacklist = new();

    // Player //
    private bool canMove = true;
    private Vector3Int latestMovement = Vector3Int.zero;

    void Awake()
    {
        // Singleton (LevelManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        // DontDestroyOnLoad(GameObject.Find("Game View")); // maybe?

        // Getting grids and tilemap references
        // SceneManager.sceneLoaded += TryGetSceneReferences;
        TryGetSceneReferences();

        // Getting tile references
        wallTile = Resources.Load<WallTile>("Tiles/Wall");
        boxTile = Resources.Load<BoxTile>("Tiles/Box");
        hexagonTile = Resources.Load<HexagonTile>("Tiles/Hexagon");
        circleTile = Resources.Load<CircleTile>("Tiles/Circle");
        areaTile = Resources.Load<AreaTile>("Tiles/Area");
        hazardTile = Resources.Load<HazardTile>("Tiles/Hazard");

        // LoadLevel("hazards");
    }

    // Gets the scene references for later use (should be called every time on scene change (actually no i lied))
    private void TryGetSceneReferences()
    {
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject != null ? gridObject.GetComponent<Grid>() : null;
        tilemapCollideable = gridObject != null ? gridObject.Find("Collideable").GetComponent<Tilemap>() : null;
        tilemapObjects = gridObject != null ? gridObject.Find("Objects").GetComponent<Tilemap>() : null;
        tilemapOverlaps = gridObject != null ? gridObject.Find("Overlaps").GetComponent<Tilemap>() : null;
    }

    // Adds a tile to the private objects list
    public void AddToObjectList(GameTile tile)
    {
        if (!typesObjectList.Contains(tile.GetTileType())) return;
        else if (!levelObjects.Contains(tile)) levelObjects.Add(tile);
    }

    // Adds a tile to the private overlaps list
    public void AddToOverlapList(GameTile tile)
    {
        if (!typesOverlapsList.Contains(tile.GetTileType())) return;
        else if (!levelOverlaps.Contains(tile)) levelOverlaps.Add(tile);
    }

    // Adds a tile to the private collideable list
    public void AddToCollideableList(GameTile tile)
    {
        if (!typesSolidsList.Contains(tile.GetTileType())) return;
        else if (!levelSolids.Contains(tile)) levelSolids.Add(tile);
    }

    // Saves a level to the game's persistent path
    public void SaveLevel(string levelName)
    {
        if (IsStringEmptyOfNull(levelName)) return;
        levelName = levelName.Trim();

        // Create the level object
        SerializableLevel level = new() { levelName = levelName };

        // Populate the level
        levelSolids.ForEach(tile => level.tiles.solidTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelObjects.ForEach(tile => level.tiles.objectTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelOverlaps.ForEach(tile => level.tiles.overlapTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));

        // Save the level locally
        string levelPath = $"{Application.persistentDataPath}/{levelName}.level";
        File.WriteAllText(levelPath, JsonUtility.ToJson(level, true));
        Debug.Log($"Saved level \"{levelName}\" to {levelPath}.");
    }

    // Load and build a level
    public void LoadLevel(string levelName)
    {
        if (IsStringEmptyOfNull(levelName)) return;
        levelName = levelName.Trim();

        // Clears the current level
        levelGrid.GetComponentsInChildren<Tilemap>().ToList().ForEach(layer => layer.ClearAllTiles());
        latestMovement = Vector3Int.zero;
        movementBlacklist.Clear();
        levelSolids.Clear();
        levelObjects.Clear();
        levelOverlaps.Clear();

        // Loads the new level
        SerializableLevel level = GetLevel(levelName);
        if (level == null) return;

        // Loads the level
        level.tiles.solidTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position), tilemapCollideable, levelSolids));
        level.tiles.objectTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position), tilemapObjects, levelObjects));
        level.tiles.overlapTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position), tilemapOverlaps, levelOverlaps));
        Debug.Log($"Loaded level \"{levelName}\"!");
    }

    // Moves a tile (or multiple)
    public bool TryMove(Vector3Int startingPosition, Vector3Int newPosition, Vector3Int direction, bool removeFromQueue = false)
    {
        // Check if the tile is allowed to move
        GameTile tile = tilemapObjects.GetTile<GameTile>(startingPosition);
        if (!tile) return false;

        // Scene bounds (x,y always at 0)
        if (!CheckSceneInbounds(newPosition)) return false;

        // Checks if directions are null
        if (direction.y > 0 && !tile.directions.up ||
            direction.y < 0 && !tile.directions.down ||
            direction.x < 0 && !tile.directions.left ||
            direction.x > 0 && !tile.directions.right) return false;

        // Moves the tile if all collision checks pass
        newPosition = tile.CollisionHandler(newPosition, direction, tilemapObjects, tilemapCollideable);
        if (newPosition == Vector3.back || newPosition == startingPosition) return false;
        MoveTile(startingPosition, newPosition, tile);

        // Updates new current position of the tile
        tile.position = newPosition;

        // Removes from movement queue
        if (removeFromQueue) { movementBlacklist.Add(tile); }

        // Checks if the tile must be destroyed (evil)
        GameTile hazard = tilemapOverlaps.GetTile<GameTile>(newPosition);
        if (hazard != null) if (hazard.GetTileType() == ObjectTypes.Hazard) Debug.LogWarning("Owie");
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
    public void PlaceTile(GameTile tile, Tilemap tilemap, List<GameTile> tileList = null)
    {
        tilemap.SetTile(tile.position, tile);
        tileList.Add(tile);
    }

    // Creates a gametile
    public GameTile CreateTile(string type, Directions defaultDirections, Vector3Int defaultPosition)
    {
        // Instantiate correct tile
        GameTile tile = type switch
        {
            "Circle" => Instantiate(circleTile),
            "Hexagon" => Instantiate(hexagonTile),
            "Wall" => Instantiate(wallTile),
            "Area" => Instantiate(areaTile),
            "Hazard" => Instantiate(hazardTile),
            _ => Instantiate(boxTile) // Default, also covers box types
        };

        // Apply tile defaults
        tile.directions = defaultDirections;
        tile.position = defaultPosition;
        return tile;
    }

    // Returns if a position is inside or outside the level bounds
    public bool CheckSceneInbounds(Vector3Int position)
    {
        return !(position.x < 0 || position.x > boundsX || position.y > 0 || position.y < boundsY);
    }

    // Applies gravity using a direction
    private void ApplyGravity(Vector3Int movement)
    {
        // Clears blacklist
        movementBlacklist.Clear();

        // Moves every object
        levelObjects.ForEach(
            tile => {
                if (!movementBlacklist.Contains(tile))
                    TryMove(tile.position, tile.position + movement, movement, true);
            }
        );
    }

    // Checks if you've won
    private void CheckCompletion()
    {
        // Condition: All area tiles have some object overlapping them, at least 1 exists
        bool winCondition = levelOverlaps.All(area =>
            { return area.GetTileType() != ObjectTypes.Area || tilemapObjects.GetTile<GameTile>(area.position) != null; })
            && levelOverlaps.Any(area => area.GetTileType() == ObjectTypes.Area);

        if (winCondition) Debug.LogWarning("Win!");
    }

    // Returns if currently in editor
    public bool IsInEditor() { return SceneManager.GetActiveScene().name == "Level Editor"; }

    // Is string empty or null
    public bool IsStringEmptyOfNull(string str) { return str == null || str == string.Empty; }

    // Gets a level and returns it as a serialized object
    private SerializableLevel GetLevel(string levelName)
    {
        TextAsset level = Resources.Load<TextAsset>($"Levels/{levelName}");
        if (!level) { Debug.LogError($"Invalid level! ({levelName})"); return null; }

        string levelJson = level.text;
        return JsonUtility.FromJson<SerializableLevel>(levelJson);
    }

    // Player Input //

    // Movement
    private void OnMove(InputValue ctx)
    {
        if (IsInEditor()) return;

        // Bad input prevention logic
        Vector3Int movement = Vector3Int.RoundToInt(ctx.Get<Vector2>());
        if (movement == Vector3Int.zero) { canMove = true; return; };
        if (!canMove) return;

        // Checks if you can actually move in that direction
        if (latestMovement == movement * -1) return;
        latestMovement = movement;
        canMove = false;

        // Moves tiles and checks for a win
        ApplyGravity(movement);
        CheckCompletion();
    }

    // Wait
    private void OnWait()
    {
        if (latestMovement == Vector3Int.zero || IsInEditor()) return;

        // Moves tiles using the user's latest movement
        ApplyGravity(latestMovement);
        CheckCompletion();
    }
}
