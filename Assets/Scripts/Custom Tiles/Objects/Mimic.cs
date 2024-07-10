using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Objects/Mimic Tile")]
public class MimicTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Mimic; }

    // Checks colisions between collideables and objects (VERY BUGGY!!! DO NOT SHIP (yet))
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {        
        // Inverts direction axis if moving normally
        if (!beingPushed) {
            direction.x *= -1;
            direction.y *= -1;
            direction.z *= -1;
            checkPosition += direction * 2; // inverts check position aswell

            // Checks if the movement is inbounds
            if (!LevelManager.Instance.CheckSceneInbounds(checkPosition)) return Vector3Int.back;
        }

        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Wall? Stop.
        if (collideableCollision) return Vector3Int.back;

        // Object? uh.
        if (objectCollision)
        {
            if (!beingPushed)
            {
                // Allow the object infront to move first (if they can)
                if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true, false))
                {
                    // Has the object moved? Fucking no. Try to push the object infront.
                    if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, false, true)) return Vector3Int.back;
                }

            } else {
                // Push if being pushed
                if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, false, true)) return Vector3Int.back;
            }
        }
        return checkPosition;
    }
    
    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = true;
        directions.editorPushable = true;
        directions.editorMinimumDirections = 0;
    }
}