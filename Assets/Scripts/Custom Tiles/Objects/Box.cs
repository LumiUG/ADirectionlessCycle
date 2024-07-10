using UnityEngine;

[CreateAssetMenu(menuName = "Game Tiles/Objects/Box Tile")]
public class BoxTile : GameTile
{
    // Returns the tile type
    public override ObjectTypes GetTileType() { return ObjectTypes.Box; }
    
    // Prepares editor variables.
    public override void PrepareTile()
    {
        directions.editorDirections = true;
        directions.editorPushable = true;
        directions.editorMinimumDirections = 0;
    }
}