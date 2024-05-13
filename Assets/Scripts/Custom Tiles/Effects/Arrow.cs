using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Effects/Arrow Tile")]
public class ArrowTile : EffectTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Arrow; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // The tile's effect
    public override void Effect(GameTile tile)
    {
        // Can you collect the arrow? dunno
        if (!((tile.directions.up == false && directions.up == true) ||
            (tile.directions.down == false && directions.down == true) ||
            (tile.directions.left == false && directions.left == true) ||
            (tile.directions.right == false && directions.right == true))) return;

        // Update directions
        if (directions.up && !tile.directions.up) { tile.directions.up = true; directions.up = false; }
        if (directions.down && !tile.directions.down) { tile.directions.down = true; directions.down = false; }
        if (directions.left && !tile.directions.left) { tile.directions.left = true; directions.left = false; }
        if (directions.right && !tile.directions.right) { tile.directions.right = true; directions.right = false; }

        // Update sprites of the object that triggered this
        tile.directions.UpdateSprites();
        LevelManager.Instance.RefreshObjectTile(tile);

        // Delete or update itself
        if (!directions.up && !directions.down && !directions.left && !directions.right) LevelManager.Instance.RemoveTile(this);
        else {
            directions.UpdateSprites();
            LevelManager.Instance.RefreshEffectTile(this);
        }
    }
}