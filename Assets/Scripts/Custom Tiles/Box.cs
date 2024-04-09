using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Box Tile")]
public class BoxTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Box; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable)
    {
        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Check for other objects infront! Recursion! (needs changes to work with other mechanics)
        if (collideableCollision || (objectCollision && !objectCollidedWith.directions.pushable)) return Vector3Int.back;
        else if (objectCollision) if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true)) return Vector3Int.back;
        return checkPosition;
    }
}
