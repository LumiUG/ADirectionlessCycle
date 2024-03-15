using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Box Tile")]
public class BoxTile : TileBase
{
    public Sprite tileSprite;
    public GameObject tileObject;

    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;
    }

    // Returns the tile type
    public LevelManager.ObjectTypes GetTileType() { return LevelManager.ObjectTypes.Box; }
}
