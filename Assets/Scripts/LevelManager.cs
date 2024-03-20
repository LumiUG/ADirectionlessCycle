using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    // Basic //
    [HideInInspector] public static LevelManager Instance;
    private Grid levelGrid;
    private Tilemap tilemapCollideable;
    private Tilemap tilemapObjects;
    private TileBase basicTile;
    private GameTile boxTile;

    // Level data //
    private List<GameTile> levelObjects = new();
    private List<GameTile> movementBlacklist = new();

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

        // Getting tile references
        basicTile = Resources.Load<TileBase>("Tiles/Default");
        boxTile = Resources.Load<BoxTile>("Tiles/Box");


        // TESTING
        tilemapCollideable.SetTile(new Vector3Int(0, 0, 0), basicTile);

        GameTile tile1 = Instantiate(boxTile);
        GameTile tile2 = Instantiate(boxTile);
        GameTile tile3 = Instantiate(boxTile);

        // Tile 1 (5, -5)
        tile1.position = new Vector3Int(5, -5, 0);
        tilemapObjects.SetTile(tile1.position, tile1);
        levelObjects.Add(tilemapObjects.GetTile<GameTile>(tile1.position));

        // Tile 2 (5, -7)
        tile2.position = new Vector3Int(5, -7, 0);
        tilemapObjects.SetTile(tile2.position, tile2);
        levelObjects.Add(tilemapObjects.GetTile<GameTile>(tile2.position));

        // Tile 3 (5, -8)
        tile3.position = new Vector3Int(5, -8, 0);
        tilemapObjects.SetTile(tile3.position, tile3);
        levelObjects.Add(tilemapObjects.GetTile<GameTile>(tile3.position));

        SaveLevel("test");
    }

    public void AddToObjectList(GameTile tile) { levelObjects.Add(tile); }

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

        // Moves the tile if all collision checks pass
        if (CheckObjectCollision(GameTile.ObjectTypes.Box, newPosition, direction)) return false; // Migrate ObjectType later
        tilemapObjects.SetTile(newPosition, tile);
        tilemapObjects.SetTile(startingPosition, null); // Deletes the old tile

        // Updates new current position of the tile
        tile.position = newPosition;

        // Removes from movement queue
        if (removeFromQueue) { movementBlacklist.Add(tile); }
        return true;
    }

    // Checks colissions between collideables and objects
    protected bool CheckObjectCollision(GameTile.ObjectTypes objectType, Vector3Int checkPosition, Vector3Int direction)
    {
        // Get the collissions
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = tilemapObjects.GetTile(checkPosition) != null;

        // Different collision handler for all objects
        switch (objectType)
        {
            case GameTile.ObjectTypes.Box: // Check for other objects infront! Recursion! (wont work with other mechanics)
                if (collideableCollision) return true;
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
    }
}
