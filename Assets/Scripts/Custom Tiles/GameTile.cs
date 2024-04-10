using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameTile : TileBase
{
    // Tilebase defaults //
    public Sprite tileSprite;
    public GameObject tileObject;

    // Tile default properties //
    public enum ObjectTypes { Box, Circle, Hexahedron, Area }
    public Vector3Int position = new();
    public Directions directions = new(true, true, true, true);

    // Sets the default tile data
    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        // Default tilebase rendering
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;

        // Adds itself to the level manager's level objects/areas/etc list
        if (!LevelManager.Instance) return;
        LevelManager.Instance.AddToObjectList(this);
        LevelManager.Instance.AddToAreaList(this);

        // Find object's custom properties references
        if (!tileData.gameObject) return;
        directions.GetSpriteReferences(tileObject);

        // Updates the sprites for the first time
        directions.UpdateSprites();
    }

    // Returns the tile type.
    public abstract ObjectTypes GetTileType();

    // DEFAULT //

    // Checks colisions between collideables and objects
    public virtual Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable)
    {
        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Checks if it is able to move
        if (collideableCollision || (objectCollision && !objectCollidedWith.directions.pushable)) return Vector3Int.back;

        // "Pushes" objects infront. Recursion!
        else if (objectCollision) if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true)) return Vector3Int.back;
        return checkPosition;
    }
}
