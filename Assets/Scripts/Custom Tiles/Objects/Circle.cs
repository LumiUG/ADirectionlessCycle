using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Objects/Circle Tile")]
public class CircleTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Circle; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable)
    {
        // Collision checks
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = tilemapObjects.GetTile<GameTile>(checkPosition) != null;

        // Check if the position is valid, if not, return the same tile you're at
        if (collideableCollision || !LevelManager.Instance.CheckSceneInbounds(checkPosition)) return checkPosition - direction;

        // Moves the tile infront if able, this DOES NOT PUSH.
        else if (objectCollision) if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true)) return checkPosition - direction;

        // Find next jump spot. Recursion...
        return CollisionHandler(checkPosition + direction, direction, tilemapObjects, tilemapCollideable);
    }
}
