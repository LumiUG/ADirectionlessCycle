using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    private readonly List<GameTile> levelObjects = new();
    private readonly List<GameTile> levelAreas = new();
    private readonly List<GameTile> movementBlacklist = new();

    // Player //
    private bool canMove = true;
    private Vector3Int latestMovement = Vector3Int.zero;

    void Awake()
    {
        // Singleton (LevelManager has no persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Getting grids and tilemap references
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject.GetComponent<Grid>();
        tilemapCollideable = gridObject.Find("Collideable").GetComponent<Tilemap>();
        tilemapObjects = gridObject.Find("Objects").GetComponent<Tilemap>();
        tilemapOverlaps = gridObject.Find("Overlaps").GetComponent<Tilemap>();

        // Getting tile references
        wallTile = Resources.Load<WallTile>("Tiles/Wall");
        boxTile = Resources.Load<BoxTile>("Tiles/Box");
        hexahedronTile = Resources.Load<HexahedronTile>("Tiles/Hexahedron");
        circleTile = Resources.Load<CircleTile>("Tiles/Circle");
        areaTile = Resources.Load<AreaTile>("Tiles/Area");


        // TESTING
        // tilemapCollideable.SetTile(new Vector3Int(0, 0, 0), wallTile);

        // GameTile tile1 = Instantiate(boxTile);
        // GameTile tile2 = Instantiate(boxTile);
        // GameTile tile3 = Instantiate(boxTile);

        // GameTile hex1 = Instantiate(hexahedronTile);

        // GameTile circle1 = Instantiate(circleTile);

        // GameTile area1 = Instantiate(areaTile);
        // GameTile area2 = Instantiate(areaTile);

        // // Tile 1 (5, -5)
        // tile1.position = new Vector3Int(5, -5, 0);
        // tilemapObjects.SetTile(tile1.position, tile1);

        // // Tile 2 (5, -7)
        // tile2.directions.pushable = false;
        // tile2.position = new Vector3Int(5, -7, 0);
        // tilemapObjects.SetTile(tile2.position, tile2);

        // // Tile 3 (5, -8)
        // tile3.position = new Vector3Int(5, -8, 0);
        // tilemapObjects.SetTile(tile3.position, tile3);

        // // Hex 1 (8, -2)
        // hex1.position = new Vector3Int(8, -2, 0);
        // hex1.directions.SetNewDirections(true, true, false, false);
        // tilemapObjects.SetTile(hex1.position, hex1);

        // // Circle 1 (6, -2)
        // circle1.position = new Vector3Int(6, -2, 0);
        // tilemapObjects.SetTile(circle1.position, circle1);

        // // Area 1 (6, -6)
        // area1.position = new Vector3Int(6, -6, 0);
        // tilemapOverlaps.SetTile(area1.position, area1);

        // // Area 2 (7, -6)
        // area2.position = new Vector3Int(7, -6, 0);
        // tilemapOverlaps.SetTile(area2.position, area2);

        // // Unused FOR NOW, level saving and loading. //
        // LoadLevel("test");
        // // SaveLevel("test");
    }

    // Adds a tile to the private objects list
    public void AddToObjectList(GameTile tile)
    {
        if (tile.GetTileType() == ObjectTypes.Area) return;
        else if (!levelObjects.Contains(tile)) levelObjects.Add(tile);
    }

    // Adds a tile to the private areas list
    public void AddToAreaList(GameTile tile)
    {
        if (tile.GetTileType() != ObjectTypes.Area) return;
        else if (!levelAreas.Contains(tile)) levelAreas.Add(tile);
    }

    // Saves a level to the game's persistent path
    public void SaveLevel(string levelName)
    {
        // Hell yeah.
        File.WriteAllText($"{Application.persistentDataPath}/{levelName}.level", JsonUtility.ToJson(levelName));
    }

    // Load and build a level
    public void LoadLevel(string level)
    {
        levelGrid.GetComponentsInChildren<Tilemap>().ToList().ForEach(layer => layer.ClearAllTiles());
        Debug.LogWarning(Resources.Load<TextAsset>($"Levels/{level}.json").text);
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
        if (levelAreas.All(area => { return tilemapObjects.GetTile<GameTile>(area.position) != null; }))
            Debug.LogWarning("Win!");
    }

    // Returns if currently in editor
    public bool IsInEditor() { return SceneManager.GetActiveScene().name == "Level Editor"; }

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
