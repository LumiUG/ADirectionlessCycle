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
        if (tile) return;

        // Checks if there is a string
        string[] stringCheck = customText.Split(";");
        if (stringCheck.Length < 2) return;

        // Checks if the dialog exists
        DialogScriptable dialogCheck = Resources.Load<DialogScriptable>($"Dialog/{stringCheck.GetValue(0)}");
        if (dialogCheck == null) return;

        // Play the dialog (too many load calls...)
        DialogManager.Instance.StartDialog(dialogCheck);
    }

    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}
