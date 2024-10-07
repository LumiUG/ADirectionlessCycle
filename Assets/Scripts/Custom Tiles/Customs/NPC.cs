using UnityEngine;
using UnityEngine.Tilemaps;

// !! NPC TILE USES CUSTOM TEXT SLICING !!
// Example: "dialogScriptable;yourSprite;triggerOnEnter;ignoreOutboundInteracts"
// Example: "Dialog/Debug;NPC/Gummi;1;0"
// Custom Values: "yourSprite" can be set to "Invisible" (caps!)

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
        // Checks if there is a string
        string[] stringCheck = customText.Split(";");
        if (stringCheck.Length != 4) return; // TODO: move to 4 for support

        // Collission check
        if (tile && stringCheck[2] == "0") return;

        // Outbound interaction (ignore?)
        if (tile == null && stringCheck[3] == "1") return;

        // Checks if the dialog exists (too many load calls...)
        DialogScriptable dialogCheck = Resources.Load<DialogScriptable>($"Dialog/{stringCheck.GetValue(0)}");
        if (dialogCheck == null) return;

        // Play the dialog 
        DialogManager.Instance.StartDialog(dialogCheck, $"Dialog/{stringCheck.GetValue(0)}");
    }

    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}
