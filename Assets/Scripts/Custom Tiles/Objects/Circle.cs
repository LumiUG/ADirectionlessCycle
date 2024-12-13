using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Objects/Circle Tile")]
public class CircleTile : GameTile
{
    private int steps = 0; // Used for recursion steps (avoiding stackoverflows)

    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Circle; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        steps++;

        // Avoiding stackoverflows with freeroam levels
        if (steps >= 28) { steps = 0; return checkPosition - direction; }

        // Collision checks
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = tilemapObjects.GetTile<GameTile>(checkPosition) != null;

        // Check if the position is valid, if not, return the same tile you're at
        if (collideableCollision || !LevelManager.Instance.CheckSceneInbounds(checkPosition)) { steps = 0; return checkPosition - direction; }

        // Moves the tile infront if able, this DOES NOT PUSH.
        if (objectCollision)
        {
            if (!beingPushed) {
                if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true)) { steps = 0; return checkPosition - direction; }
            }
            else {
                // Push if being pushed
                if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, false, true)) { steps = 0; return checkPosition - direction; }
            }
        }

        // If pushed, move infront instead
        if (beingPushed) { steps = 0; return checkPosition; }

        // Find next jump spot. Recursion...
        return CollisionHandler(checkPosition + direction, direction, tilemapObjects, tilemapCollideable);
    }
    
    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = true;
        directions.editorPushable = true;
        directions.editorMinimumDirections = 0;
    }
}
