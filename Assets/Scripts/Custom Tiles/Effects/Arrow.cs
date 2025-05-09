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

        // Update directions (im sorry)
        if (directions.up && !tile.directions.up) { tile.directions.SetNewDirections(true, tile.directions.down, tile.directions.left, tile.directions.right); directions.SetNewDirections(false, directions.down, directions.left, directions.right); }
        if (directions.down && !tile.directions.down) { tile.directions.SetNewDirections(tile.directions.up, true, tile.directions.left, tile.directions.right); directions.SetNewDirections(directions.up, false, directions.left, directions.right); }
        if (directions.left && !tile.directions.left) { tile.directions.SetNewDirections(tile.directions.up, tile.directions.down, true, tile.directions.right); directions.SetNewDirections(directions.up, directions.down, false, directions.right); }
        if (directions.right && !tile.directions.right) { tile.directions.SetNewDirections(tile.directions.up, tile.directions.down, tile.directions.left, true); directions.SetNewDirections(directions.up, directions.down, directions.left, false); }

        // Update sprites of the object that triggered this
        LevelManager.I.RefreshObjectTile(tile);

        // Delete or update itself
        if (!directions.up && !directions.down && !directions.left && !directions.right) LevelManager.I.RemoveTile(this);
        else LevelManager.I.RefreshEffectTile(this);
    }
    
    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = true;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 1;
    }
}