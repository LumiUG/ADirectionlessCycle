using UnityEngine;

[CreateAssetMenu(menuName = "Game Tiles/Circle Tile")]
public class CircleTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Circle; }
}
