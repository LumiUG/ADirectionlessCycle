using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using static GameTile;

public class LevelManager : MonoBehaviour
{
    // Basic //
    [HideInInspector] public static LevelManager Instance;
    public int boundsX = 19;
    public int boundsY = -11;

    private Grid levelGrid;
    private Tilemap tilemapCollideable;
    private Tilemap tilemapObjects;
    private Tilemap tilemapOverlaps;
    private TileBase basicTile;
    private GameTile boxTile;
    private GameTile areaTile;

    // Level data //
    private readonly List<GameTile> levelObjects = new();
    private readonly List<GameTile> levelAreas = new();
    private readonly List<GameTile> movementBlacklist = new();

    // Player //
    private bool canMove = true;

    void Awake()
    {
        // Singleton
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Getting grids and tilemap references
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject.GetComponent<Grid>();
        tilemapCollideable = gridObject.Find("Collideable").GetComponent<Tilemap>();
        tilemapObjects = gridObject.Find("Objects").GetComponent<Tilemap>();
        tilemapOverlaps = gridObject.Find("Overlaps").GetComponent<Tilemap>();

        // Getting tile references
        basicTile = Resources.Load<TileBase>("Tiles/Default");
        boxTile = Resources.Load<BoxTile>("Tiles/Box");
        areaTile = Resources.Load<AreaTile>("Tiles/Area");


        // TESTING
        GameTile tile1 = Instantiate(boxTile);
        GameTile tile2 = Instantiate(boxTile);
        GameTile tile3 = Instantiate(boxTile);

        GameTile area1 = Instantiate(areaTile);
        GameTile area2 = Instantiate(areaTile);

        // Tile 1 (5, -5)
        tile1.position = new Vector3Int(5, -5, 0);
        tile1.directions.SetNewDirections(true, true, false, false);
        tilemapObjects.SetTile(tile1.position, tile1);

        // Tile 2 (5, -7)
        tile2.position = new Vector3Int(5, -7, 0);
        tilemapObjects.SetTile(tile2.position, tile2);

        // Tile 3 (5, -8)
        tile3.position = new Vector3Int(5, -8, 0);
        tilemapObjects.SetTile(tile3.position, tile3);

        // Area 1 (6, -6)
        area1.position = new Vector3Int(6, -6, 0);
        tilemapOverlaps.SetTile(area1.position, area1);

        // Area 2 (7, -6)
        area2.position = new Vector3Int(7, -6, 0);
        tilemapOverlaps.SetTile(area2.position, area2);

        // Unused FOR NOW, level saving and loading. //
        // LoadLevel("test");
        // SaveLevel("test");
    }

    // Adds a tile to the private objects list
    public void AddToObjectList(GameTile tile)
    {
        if (tile.GetTileType() != ObjectTypes.Box) return;
        else if (!levelObjects.Contains(tile)) levelObjects.Add(tile);
    }

    // Adds a tile to the private areas list
    public void AddToAreaList(GameTile tile)
    {
        if (tile.GetTileType() != ObjectTypes.Area) return;
        else if (!levelAreas.Contains(tile)) levelAreas.Add(tile);
    }

    // Saves a level to the game's persistent path
    private void SaveLevel(string level)
    {
        // Default status
        // var data = JsonUtility.FromJson(Resources.Load<TextAsset>("MainData").text);
        var test = new { a = "a", b = 1 };
        Debug.Log($"{test.a}, {test.b}, {test.GetType()}");
        File.WriteAllText($"{Application.persistentDataPath}/{level}.level", JsonUtility.ToJson(test));
    }

    // Load and build a level
    private void LoadLevel(string level)
    {
        levelGrid.GetComponentsInChildren<Tilemap>().ToList().ForEach(layer => layer.ClearAllTiles());
        Debug.LogWarning(Resources.Load($"Levels/{level}").name);
    }

    // Moves a tile (or multiple)
    protected bool MoveTile(Vector3Int startingPosition, Vector3Int newPosition, Vector3Int direction, bool removeFromQueue = false)
    {
        // Check if the tile is allowed to move
        GameTile tile = tilemapObjects.GetTile<GameTile>(startingPosition);
        if (!tile) return false;

        // Scene bounds (x,y always at 0)
        if (newPosition.x < 0 || newPosition.x > boundsX || newPosition.y > 0 || newPosition.y < boundsY) return false;
 
        // up: (0, 1, 0)
        // down: (0, -1, 0)
        // left: (-1, 0, 0)
        // right: (1, 0, 0)
        if (direction.y > 0 && !tile.directions.up ||
            direction.y < 0 && !tile.directions.down ||
            direction.x < 0 && !tile.directions.left ||
            direction.x > 0 && !tile.directions.right) return false;


        // Moves the tile if all collision checks pass
        if (CheckObjectCollision(tile.GetTileType(), newPosition, direction)) return false;
        tilemapObjects.SetTile(newPosition, tile);
        tilemapObjects.SetTile(startingPosition, null); // Deletes the old tile

        // Updates new current position of the tile
        tile.position = newPosition;

        // Removes from movement queue
        if (removeFromQueue) { movementBlacklist.Add(tile); }
        return true;
    }

    // Checks colissions between collideables and objects
    protected bool CheckObjectCollision(ObjectTypes objectType, Vector3Int checkPosition, Vector3Int direction)
    {
        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Different collision handler for all objects
        switch (objectType)
        {
            case ObjectTypes.Box: // Check for other objects infront! Recursion! (needs changes to work with other mechanics)
                if (collideableCollision || (objectCollision && !objectCollidedWith.canBePushed)) return true;
                else if (objectCollision) return !MoveTile(checkPosition, checkPosition + direction, direction, true);
                return false;

            default:
                return false;
        }
    }

    // Player Input //
    private void OnMove(InputValue ctx)
    {
        Vector2Int movement = Vector2Int.RoundToInt(ctx.Get<Vector2>());
        if (movement == Vector2Int.zero) { canMove = true; return; };
        if (!canMove) return;
        canMove = false;

        // Moves all boxes in a direction
        movementBlacklist.Clear();
        levelObjects.ForEach(tile => {
            if (!movementBlacklist.Contains(tile))
                MoveTile(tile.position, tile.position + (Vector3Int)movement, (Vector3Int)movement, true);
            }
        );

        // Areas check for overlaps
        if (levelAreas.All(area => { return tilemapObjects.GetTile<GameTile>(area.position) != null; }))
            Debug.LogWarning("Win!");
    }
}
