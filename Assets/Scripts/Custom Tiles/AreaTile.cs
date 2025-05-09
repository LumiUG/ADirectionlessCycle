using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class AreaTile : GameTile
{
    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // Pings a tile
    public void Ping()
    {
        tileObject.GetComponent<SpriteRenderer>().sprite = tileSprite;
        LevelManager.I.RefreshAreaTile(this);
    }
}
