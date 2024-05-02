using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Effects/Invert Tile")]
public class InvertTile : EffectTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Invert; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // The tile's effect
    public override void Effect(GameTile tile)
    {
        tile.directions.up = !tile.directions.up;
        tile.directions.down = !tile.directions.down;
        tile.directions.left = !tile.directions.left;
        tile.directions.right = !tile.directions.right;
        tile.directions.UpdateSprites();
        LevelManager.Instance.RefreshObjectTile(tile);
    }
}