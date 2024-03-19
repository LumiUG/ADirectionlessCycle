using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

public class LevelManager : MonoBehaviour
{
    public enum ObjectTypes { Box }

    // Basic //
    [HideInInspector] public static LevelManager Instance;
    private Grid levelGrid;
    private Tilemap tilemapCollideable;
    private Tilemap tilemapObjects;
    private TileBase basicTile;
    private GameTile boxTile;

    // Level data //
    private List<GameTile> levelObjects = new();

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

        // Tile 1 (5, -5)
        tile1.position = new Vector3Int(5, -5, 0);
        tilemapObjects.SetTile(tile1.position, tile1);
        levelObjects.Add(tilemapObjects.GetTile<GameTile>(tile1.position));

        // Tile 2 (5, -7)
        tile2.position = new Vector3Int(5, -7, 0);
        tilemapObjects.SetTile(tile2.position, tile2);
        levelObjects.Add(tilemapObjects.GetTile<GameTile>(tile2.position));

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
    protected bool MoveTile(Vector3Int startingPosition, Vector3Int newPosition)
    {
        if (CheckObjectCollision(ObjectTypes.Box, newPosition)) return false; // Migrate ObjectType later

        // Moves the tile if all collision checks pass
        GameTile tile = tilemapObjects.GetTile<GameTile>(startingPosition);
        tilemapObjects.SetTile(newPosition, tile);
        tilemapObjects.SetTile(startingPosition, null); // Deletes the old tile

        // Updates new current position of the tile
        tile.position = newPosition;
        return true;
    }

    // Checks colissions between collideables and objects
    protected bool CheckObjectCollision(ObjectTypes objectType, Vector3Int checkPosition)
    {
        // Get the collissions
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = tilemapObjects.GetTile(checkPosition) != null;

        // Different collision handler for all objects
        switch (objectType)
        {
            case ObjectTypes.Box: // Check for other objects infront! Recursion!
                return collideableCollision;
            default:
                return false;
        }
    }

    private void OnMove(InputValue ctx)
    {
        Vector2Int movement = Vector2Int.RoundToInt(ctx.Get<Vector2>());
        if (movement == Vector2Int.zero) { canMove = true; return; };

        // Moves all boxes in a direction
        if (!canMove) return;
        levelObjects.ForEach(tile =>MoveTile(tile.position, tile.position + (Vector3Int)movement));
        canMove = false;
    }
}
