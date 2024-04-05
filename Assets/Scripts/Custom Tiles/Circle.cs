using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Circle Tile")]
public class CircleTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Circle; }

    // Checks colisions between collideables and objects
    public override bool CollisionHandler(GameTile tile, Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable)
    {
        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Check for other objects infront! Recursion! (needs changes to work with other mechanics)
        if (collideableCollision || (objectCollision && !objectCollidedWith.directions.pushable)) return true;
        else if (objectCollision) return !LevelManager.Instance.MoveTile(checkPosition, checkPosition + direction, direction, true);
        return false;
    }
}
