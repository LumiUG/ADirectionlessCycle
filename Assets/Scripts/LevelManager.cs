using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

public class LevelManager : MonoBehaviour
{
    private Grid levelGrid;
    private Tilemap tilemapCollideable;
    private TileBase basicTile;

    void Start()
    {
        // Getting grids and tilemap references
        Transform gridObject = transform.Find("Level Grid");
        levelGrid = gridObject.GetComponent<Grid>();
        tilemapCollideable = gridObject.Find("Collideable").GetComponent<Tilemap>();

        // Getting tile references
        basicTile = Resources.Load<TileBase>("Tiles/Default");
        Debug.Log(basicTile.name);

        // Set default tile at 0,0 (testing)
        tilemapCollideable.SetTile(new Vector3Int(0, 0, 0), basicTile);
        // SaveLevel("test");
    }


    public void SaveLevel(string level)
    {
        // Default status
        // var data = JsonUtility.FromJson(Resources.Load<TextAsset>("MainData").text);
        var test = new { a = "a", b = 1 };
        File.WriteAllText($"{Application.persistentDataPath}/{level}.level", JsonUtility.ToJson(test));
    }

    public void LoadLevel(string level)
    {
        levelGrid.GetComponentsInChildren<Tilemap>().ToList().ForEach(layer => layer.ClearAllTiles());
        Debug.LogWarning(Resources.Load($"Levels/{level}").name);
    }
}
