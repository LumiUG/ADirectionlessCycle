using UnityEngine;

[CreateAssetMenu(menuName = "Game Tiles/Area Tile")]
public class AreaTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Area; }
}
