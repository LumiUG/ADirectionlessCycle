using UnityEngine;
using UnityEngine.Tilemaps;

// !! NPC TILE USES CUSTOM TEXT SLICING !!
// Example: "dialogScriptable;yourSprite;triggerOnEnter;ignoreOutboundInteracts"
// Example: "Debug;NPC/Gummi;1;0"
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
        if (stringCheck.Length != 4) return;

        // Collission check
        if (tile && stringCheck[2] == "0") return;

        // Outbound interaction (ignore?)
        if (tile == null && stringCheck[3] == "1") return;

        // Vanilla handling
        if (!$"{stringCheck.GetValue(0)}".StartsWith("{") && !$"{stringCheck.GetValue(0)}".EndsWith("}"))
        {
            // Checks if the dialog exists (too many load calls...)
            DialogScriptable dialogCheck = Resources.Load<DialogScriptable>($"Dialog/{stringCheck.GetValue(0)}");
            if (dialogCheck == null) return;

            // Play the dialog 
            DialogManager.I.StartDialog(dialogCheck, $"Dialog/{stringCheck.GetValue(0)}");
            return;
        }

        // Userscript (custom user handling)
        DialogScriptable userDialog = CreateInstance<DialogScriptable>();
        JsonUtility.FromJsonOverwrite($"{stringCheck.GetValue(0)}", userDialog);

        string customID = userDialog.dialog.Length <= 0 ? (userDialog.dialog[0].Length >= 4 ? userDialog.dialog[0][..4] : userDialog.dialog[0]) : $"EVENT-{Random.Range(0,100)}";
        DialogManager.I.StartDialog(userDialog, $"CUSTOM-{customID}");
    }

    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = false;
        directions.editorPushable = false;
        directions.editorMinimumDirections = 0;
    }
}
