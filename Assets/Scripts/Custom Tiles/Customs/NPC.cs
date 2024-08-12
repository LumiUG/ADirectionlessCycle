using UnityEngine;
using UnityEngine.Tilemaps;

// NPC TILE USES CUSTOM TEXT SLICING !!
// Example: "dialogScriptable;yourSprite"

[CreateAssetMenu(menuName = "Game Tiles/Customs/NPC Tile")]
public class NPCTile : CustomTile 
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.NPC; }

    // Checks colisions between collideables and objects
    public override Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable, bool beingPushed = false)
    {
        return Vector3Int.back;
    }

    // The tile's effect
    public override void Effect(GameTile tile)
    {
        if (tile.directions.GetActiveDirectionCount() <= 0) return;

        // Play the dialog (too many load calls...)
        DialogManager.Instance.StartDialog(Resources.Load<DialogScriptable>($"Dialog/{customText.Split(";")[0]}"));
    }

    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    
        // Updates the NPC sprite upon loading again
        if (customText != string.Empty)
        {
            tileSprite = Resources.Load<Sprite>($"Sprites/Tiles/{customText.Split(";")[1]}");
            LevelManager.Instance.RefreshCustomTile(this);
        }
    }
}
