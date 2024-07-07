using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Areas/Inverse Area Tile")]
public class InverseWinAreaTile : AreaTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.InverseArea; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }
    
    // Prepares editor variables.
    public override void PrepareEditor()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}
