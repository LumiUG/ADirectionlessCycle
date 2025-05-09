using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameTile : TileBase
{
    // Tilebase defaults //
    public Sprite tileSprite;
    public GameObject tileObject;

    // Tile default properties //
    public enum ObjectTypes { Wall, AntiWall, Box, Circle, Hexagon, Mimic, Area, InverseArea, OutboundArea, Hazard, Void, Invert, Arrow, NegativeArrow, Orb, Fragment, Level, Hologram, NPC, Fake, Mask, Pull }
    public Vector3Int position = new();
    public Directions directions = new(true, true, true, true);

    // Sets the default tile data
    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        // Default tilebase rendering
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;

        // Find object's custom properties references
        if (!tileData.gameObject || !LevelManager.I) return;
        directions.GetSpriteReferences(tileData.gameObject);

        // Updates the sprites for the first time
        directions.UpdateSprites();
    }

    // Returns the tile type.
    public abstract ObjectTypes GetTileType();

    // Prepares editor variables.
    public abstract void PrepareTile();

    // Overlap tile
    public Sprite GetOverlapSprite()
    {
        if (!LevelManager.I) return null;
        if (GameManager.save.preferences.outlineType == "NONE") return null;

        // Full Outlines
        if (GameManager.save.preferences.outlineType == "Full")
        {
            return GetTileType() switch
            {
                ObjectTypes.Mimic => LevelManager.I.fullOverlapBox,
                ObjectTypes.Box => LevelManager.I.fullOverlapBox,
                ObjectTypes.Circle => LevelManager.I.fullOverlapCircle,
                ObjectTypes.Hexagon => LevelManager.I.fullOverlapHex,
                _ => null
            };
        }

        // Dotted Outlines
        if (GameManager.save.preferences.outlineType == "Dotted")
        {
            return GetTileType() switch
            {
                ObjectTypes.Mimic => LevelManager.I.dottedOverlapBox,
                ObjectTypes.Box => LevelManager.I.dottedOverlapBox,
                ObjectTypes.Circle => LevelManager.I.dottedOverlapCircle,
                ObjectTypes.Hexagon => LevelManager.I.dottedOverlapHex,
                _ => null
            };
        }

        return null;
    }

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
                if (!LevelManager.I.TryMove(checkPosition, checkPosition + direction, direction, true, false))
                {
                    // Has the object moved? Fucking no. Try to push the object infront.
                    if (!LevelManager.I.TryMove(checkPosition, checkPosition + direction, direction, false, true)) return Vector3Int.back;
                }

            } else {
                // Push if being pushed
                if (!LevelManager.I.TryMove(checkPosition, checkPosition + direction, direction, false, true)) return Vector3Int.back;
            }
        }
        return checkPosition;
    }
}
