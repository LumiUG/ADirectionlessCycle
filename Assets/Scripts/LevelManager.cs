using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public enum ObjectTypes { Box }
    
    // Basic //
    private Grid levelGrid;
    private Tilemap tilemapCollideable;
    private Tilemap tilemapObjects;
    private TileBase basicTile;
    private TileBase boxTile;

    // Level data //
    private List<TileBase> levelObjects;

    void Start()
    {
        // Getting grids and tilemap references
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject.GetComponent<Grid>();
        tilemapCollideable = gridObject.Find("Collideable").GetComponent<Tilemap>();
        tilemapObjects = gridObject.Find("Objects").GetComponent<Tilemap>();


        // Getting tile references
        basicTile = Resources.Load<TileBase>("Tiles/Default");
        boxTile = Resources.Load<BoxTile>("Tiles/Box");
        Debug.Log(basicTile.name);

        // Set default tile at 0,0 (testing)
        tilemapCollideable.SetTile(new Vector3Int(0, 0, 0), basicTile);
        Debug.Log(tilemapObjects.GetTile<BoxTile>(new Vector3Int(5, -5, 0)).GetTileType());

        // SaveLevel("test");
    }

    public void SaveLevel(string level)
    {
        // Default status
        // var data = JsonUtility.FromJson(Resources.Load<TextAsset>("MainData").text);
        var test = new { a = "a", b = 1 };
        File.WriteAllText($"{Application.persistentDataPath}/{level}.level", JsonUtility.ToJson(test));
    }

    // Load and build a level
    public void LoadLevel(string level)
    {
        levelGrid.GetComponentsInChildren<Tilemap>().ToList().ForEach(layer => layer.ClearAllTiles());
        Debug.LogWarning(Resources.Load($"Levels/{level}").name);
    }

    // Moves a tile (or multiple)
    public bool MoveTile(Vector3Int startingPosition, Vector3Int newPosition)
    {
        if (CheckObjectCollision(ObjectTypes.Box, newPosition)) return false; // Migrate ObjectType later

        // Moves the tile if all collision checks pass
        tilemapObjects.SetTile(newPosition, tilemapObjects.GetTile(startingPosition));
        tilemapObjects.SetTile(startingPosition, null); // Deletes the old tile
        return true;
    }

    // Checks colissions between collideables and objects
    public bool CheckObjectCollision(ObjectTypes objectType, Vector3Int checkPosition)
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveTile(new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0));
        }
    }
}
