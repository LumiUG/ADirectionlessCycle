using UnityEngine;

[CreateAssetMenu(menuName = "Game Tiles/Hexagon Tile")]
public class HexagonTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Hexagon; }
}