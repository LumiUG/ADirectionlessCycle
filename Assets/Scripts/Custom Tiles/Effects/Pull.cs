using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Effects/Pull Tile")]
public class PullTile : EffectTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Pull; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // The tile's effect
    public override void Effect(GameTile tile)
    {
        LevelManager.I.TryMove(tile.position, tile.position - LevelManager.I.currMove, -LevelManager.I.currMove, false, true);
    }
    
    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}