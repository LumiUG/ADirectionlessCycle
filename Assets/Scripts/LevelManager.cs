using UnityEngine;
using UnityEngine.Tilemaps;

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
    }

    void Update()
    {
        tilemapCollideable.SetTile(new Vector3Int(0, 0, 0), basicTile);
    }
}
