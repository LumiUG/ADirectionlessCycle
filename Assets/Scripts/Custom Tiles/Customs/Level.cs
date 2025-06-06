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

        if (customText == "Void/Trigger")
        {
            LevelManager.I.MoveTilemaps(new Vector3(0, -8));
            LevelManager.I.voidedCutscene = true;
            Actions.ExtraDiveIn("5");
            return;
        }

        // Loads a level using its custom text
        var levelTest = LevelManager.I.LoadLevel(customText);
        if (!levelTest) levelTest = LevelManager.I.LoadLevel(customText, true);
        
        if (levelTest)
        {
            if (!LevelManager.I.currentLevel.hideUI) UI.I.ingame.Toggle(true);
            LevelManager.I.worldOffsetX = 0;
            LevelManager.I.worldOffsetY = 0;
            LevelManager.I.StopMovements();
            LevelManager.I.ReloadLevel();
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
