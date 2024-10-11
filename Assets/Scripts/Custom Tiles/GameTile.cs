using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameTile : TileBase
{
    // Tilebase defaults //
    public Sprite tileSprite;
    public GameObject tileObject;

    // Tile default properties //
    public enum ObjectTypes { Wall, AntiWall, Box, Circle, Hexagon, Mimic, Area, InverseArea, OutboundArea, Hazard, Void, Invert, Arrow, NegativeArrow, Orb, Fragment, Level, Hologram, NPC, Fake }
    public Vector3Int position = new();
    public Directions directions = new(true, true, true, true);

    // Sets the default tile data
    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        // Default tilebase rendering
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;

        // Find object's custom properties references
        if (!tileData.gameObject || !LevelManager.Instance) return;
        directions.GetSpriteReferences(tileData.gameObject);

        // Updates the sprites for the first time
        directions.UpdateSprites();
    }

    // Returns the tile type.
    public abstract ObjectTypes GetTileType();

    // Prepares editor variables.
    public abstract void PrepareTile();

    // DEFAULT //

    // Checks colisions between collideables and objects
    public virtual Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
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
}
