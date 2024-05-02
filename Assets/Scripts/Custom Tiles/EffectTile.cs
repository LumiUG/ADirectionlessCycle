using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class EffectTile : GameTile
{
    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // Tile's effect
    public abstract void Effect(GameTile tile);
}
