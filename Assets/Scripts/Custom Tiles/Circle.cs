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
        // Get the collissions
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = tilemapObjects.GetTile<GameTile>(checkPosition) != null;

        // Check for other objects infront! Recursion! (needs changes to work with other mechanics)
        if (collideableCollision || objectCollision || !LevelManager.Instance.CheckSceneInbounds(checkPosition))
        { 
            return checkPosition - direction;
        }

        return CollisionHandler(checkPosition + direction, direction, tilemapObjects, tilemapCollideable);
    }
}
