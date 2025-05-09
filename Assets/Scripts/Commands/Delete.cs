using UnityEngine;

public class EditorDelete : ICommand
{
    internal EditorDelete(Vector3Int position)
    {
        this.position = position;
    }

    // Deletes a tile from the corresponding grid (holy shit kill me)
    internal override bool Execute()
    {
        GameTile tile = Editor.I.GetEditorTile(position);
        if (!tile) return false;
        
        // Delete the tile
        LevelManager.I.RemoveTile(tile);
        this.tile = tile.GetTileType();
        return true;
    }

    internal override void Undo()
    {
        new EditorPlace(tile, position).Execute();
    }
}
