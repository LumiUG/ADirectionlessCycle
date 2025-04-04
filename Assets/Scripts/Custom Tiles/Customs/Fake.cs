using UnityEngine;
using UnityEngine.Tilemaps;

// !! CUSTOM TEXT TILE !!
// Example: "yourSprite"

[CreateAssetMenu(menuName = "Game Tiles/Customs/Hologram Tile")]
public class HologramTile : CustomTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Hologram; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // The tile's effect
    public override void Effect(GameTile tile) { }

    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}
