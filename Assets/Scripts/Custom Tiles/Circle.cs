using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Circle Tile")]
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
        if (collideableCollision || objectCollision || !LevelManager.Instance.CheckSceneInbounds(checkPosition)) return checkPosition - direction;

        // Find next jump spot. Recursion...
        return CollisionHandler(checkPosition + direction, direction, tilemapObjects, tilemapCollideable);
    }
}
