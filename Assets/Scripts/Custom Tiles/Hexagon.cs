using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Hexagon Tile")]
public class HexagonTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Hexagon; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable)
    {
        // Move double
        checkPosition += direction;

        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Checks if it is able to move
        if (collideableCollision || objectCollision) return Vector3Int.back;

        // Moves if there's nothing infront
        return checkPosition;
    }
}