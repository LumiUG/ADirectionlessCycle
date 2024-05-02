using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Objects/Hexagon Tile")]
public class HexagonTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Hexagon; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        // Move double, 
        checkPosition += !beingPushed ? direction : Vector3Int.zero;
        if (!LevelManager.Instance.CheckSceneInbounds(checkPosition) && !beingPushed) return Vector3Int.back;

        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Checks if it is able to move
        if (collideableCollision) return Vector3Int.back;

        // Object collission, NEVER pushes, NEVER moves an unpushable object (except other hex)
        if (objectCollision && !beingPushed && !objectCollidedWith.directions.pushable && objectCollidedWith.GetTileType() != ObjectTypes.Hexagon)
        {
            LevelManager.Instance.AddToLateMove(this);
            return Vector3Int.back;
        }
        if (objectCollision) if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true)) return Vector3Int.back;

        // Moves if there's nothing infront
        return checkPosition;
    }
}