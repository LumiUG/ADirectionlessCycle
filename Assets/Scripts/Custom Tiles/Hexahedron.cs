using UnityEngine;

[CreateAssetMenu(menuName = "Game Tiles/Hexahedron Tile")]
public class HexahedronTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Hexahedron; }
}
