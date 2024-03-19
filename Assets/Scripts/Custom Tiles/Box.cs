using UnityEngine;

[CreateAssetMenu(menuName = "Game Tiles/Box Tile")]
public class BoxTile : GameTile
{
    // Returns the tile type
    public override LevelManager.ObjectTypes GetTileType() { return LevelManager.ObjectTypes.Box; }
}
