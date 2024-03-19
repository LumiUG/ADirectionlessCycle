using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameTile : TileBase
{
    // Tilebase defaults //
    public Sprite tileSprite;
    public GameObject tileObject;

    // Tile default properties //
    public Vector3Int position = new();
    public bool test;

    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;
    }

    // Returns the tile type.
    public abstract LevelManager.ObjectTypes GetTileType();

    private IEnumerator NotifyLevelManager()
    {
        while (!LevelManager.Instance) { yield return new WaitForSecondsRealtime(0.1f); }

        // Pings the level manager with its own object for addition to the object list.
        LevelManager.Instance.AddToObjectList(this);
        Debug.Log("added");
    }
}
