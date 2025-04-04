using UnityEngine;
using UnityEngine.Tilemaps;

// !! CUSTOM TEXT TILE !!
// Example: "levelID"

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
        var levelTest = LevelManager.Instance.LoadLevel(customText);
        if (!levelTest) levelTest = LevelManager.Instance.LoadLevel(customText, true);
        
        if (levelTest)
        {
            if (!LevelManager.Instance.currentLevel.hideUI) UI.Instance.ingame.Toggle(true);
            LevelManager.Instance.worldOffsetX = 0;
            LevelManager.Instance.worldOffsetY = 0;
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
