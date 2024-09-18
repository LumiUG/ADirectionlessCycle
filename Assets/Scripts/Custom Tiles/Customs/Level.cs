using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Game Tiles/Customs/Level Tile")]
public class LevelTile : CustomTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Level; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // The tile's effect
    public override void Effect(GameTile tile)
    {
        if (tile.directions.GetActiveDirectionCount() <= 0) return;

        // Loads a level using its custom text
        if (LevelManager.Instance.LoadLevel(customText))
        {
            if (!LevelManager.Instance.currentLevel.hideUI) UI.Instance.ingame.Toggle(true);
            LevelManager.Instance.StopMovements();
            LevelManager.Instance.ReloadLevel();
        }
    }

    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}
