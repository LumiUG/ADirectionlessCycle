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
    [HideInInspector] public static LevelManager Instance;
    [HideInInspector] public GameTile wallTile;
    [HideInInspector] public GameTile boxTile;
    [HideInInspector] public GameTile hexahedronTile;
    [HideInInspector] public GameTile circleTile;
    [HideInInspector] public GameTile areaTile;
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

        // Getting grids and tilemap references
        GetSceneReferences();

        // Getting tile references
        wallTile = Resources.Load<WallTile>("Tiles/Wall");
        boxTile = Resources.Load<BoxTile>("Tiles/Box");
        hexahedronTile = Resources.Load<HexahedronTile>("Tiles/Hexahedron");
        circleTile = Resources.Load<CircleTile>("Tiles/Circle");
        areaTile = Resources.Load<AreaTile>("Tiles/Area");

        // Loads a level //
        //LoadLevel("level");
    }

    // Gets the scene references for later use (should be called every time on scene change)
    private void GetSceneReferences()
    {
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject.GetComponent<Grid>();
        tilemapCollideable = gridObject.Find("Collideable").GetComponent<Tilemap>();
        tilemapObjects = gridObject.Find("Objects").GetComponent<Tilemap>();
        tilemapOverlaps = gridObject.Find("Overlaps").GetComponent<Tilemap>();
    }

    // Adds a tile to the private objects list
    public void AddToObjectList(GameTile tile)
    {
        if (tile.GetTileType() == ObjectTypes.Area) return;
        else if (!levelObjects.Contains(tile)) levelObjects.Add(tile);
    }

    // Adds a tile to the private overlaps list
    public void AddToAreaList(GameTile tile)
    {
        if (tile.GetTileType() != ObjectTypes.Area) return;
        else if (!levelOverlaps.Contains(tile)) levelOverlaps.Add(tile);
    }

    // Adds a tile to the private collideable list
    public void AddToCollideableList(GameTile tile)
    {
        if (tile.GetTileType() != ObjectTypes.Wall) return;
        else if (!levelSolids.Contains(tile)) levelSolids.Add(tile);
    }

    // Saves a level to the game's persistent path
    public void SaveLevel(string levelName)
    {
        // Create the level object
        SerializableLevel level = new();

        // Populate the level
        level.levelName = levelName;
        levelSolids.ForEach(tile => level.tiles.solidTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelObjects.ForEach(tile => level.tiles.objectTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));
        levelOverlaps.ForEach(tile => level.tiles.overlapTiles.Add(new(tile.GetTileType(), tile.directions, tile.position)));

        // Save the level locally
        File.WriteAllText($"{Application.persistentDataPath}/{levelName}.level", JsonUtility.ToJson(level, true));
        Debug.LogWarning("Saved!");
    }

    // Load and build a level
    public void LoadLevel(string levelName)
    {
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
        level.tiles.solidTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position), tilemapCollideable));
        level.tiles.objectTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position), tilemapObjects));
        level.tiles.overlapTiles.ForEach(tile => PlaceTile(CreateTile(tile.type, tile.directions, tile.position), tilemapOverlaps));
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
    public void PlaceTile(GameTile tile, Tilemap tilemap) { tilemap.SetTile(tile.position, tile); }

    // Creates a gametile
    public GameTile CreateTile(string type, Directions defaultDirections, Vector3Int defaultPosition)
    {
        // Instantiate correct tile
        GameTile tile;
        switch (type)
        {
            default:
            case "Box":
                tile = Instantiate(boxTile);
                break;

            case "Circle":
                tile = Instantiate(circleTile);
                break;

            case "Hexahedron":
                tile = Instantiate(hexahedronTile);
                break;

            case "Wall":
                tile = Instantiate(wallTile);
                break;

            case "Area":
                tile = Instantiate(areaTile);
                break;
        }

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
        bool winCondition = levelOverlaps.All(area =>
        { 
            if (area.GetTileType() != ObjectTypes.Area) return true;
            return tilemapObjects.GetTile<GameTile>(area.position) != null;
        });

        if (winCondition) Debug.LogWarning("Win!");
    }

    // Returns if currently in editor
    public bool IsInEditor() { return SceneManager.GetActiveScene().name == "Level Editor"; }

    // Gets a level and returns it as a serialized object
    private SerializableLevel GetLevel(string levelName)
    {
        // Debug.LogWarning(Resources.Load<TextAsset>($"Levels/{levelName.ToLower().Trim()}").text);
        string levelJson = File.ReadAllText($"{Application.persistentDataPath}/{levelName.ToLower().Trim()}.level");
        if (levelJson == string.Empty) { Debug.LogError($"Invalid level! ({levelName})"); return null; }
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
