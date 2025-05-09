using System.Linq;
using UnityEngine;
using static GameTile;

public class EditorPlace : ICommand
{
    internal EditorPlace(ObjectTypes tile, Vector3Int position)
    {
        this.tile = tile;
        this.position = position;
    }

    // Places a tile on the corresponding grid
    internal override bool Execute()
    {
        // Creates the tile (this creates a tile every frame the button is held! very bad!)
        GameTile tileToCreate = LevelManager.I.CreateTile(tile.ToString(), new(), position);

        // Sets the tile
        switch (tile)
        {
            case ObjectTypes t when LevelManager.I.typesSolidsList.Contains(t):
                if (LevelManager.I.tilemapCollideable.GetTile<GameTile>(position)) return false;
                LevelManager.I.tilemapCollideable.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.I.AddToCollideableList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.I.typesAreas.Contains(t):
                if (LevelManager.I.tilemapWinAreas.GetTile<GameTile>(position)) return false;
                LevelManager.I.tilemapWinAreas.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.I.AddToWinAreasList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.I.typesHazardsList.Contains(t):
                if (LevelManager.I.tilemapHazards.GetTile<GameTile>(position)) return false;
                LevelManager.I.tilemapHazards.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.I.AddToHazardsList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.I.typesEffectsList.Contains(t):
                if (LevelManager.I.tilemapEffects.GetTile<GameTile>(position)) return false;
                LevelManager.I.tilemapEffects.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.I.AddToEffectsList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.I.typesCustomsList.Contains(t):
                if (LevelManager.I.tilemapCustoms.GetTile<CustomTile>(position)) return false;
                CustomTile custom = (CustomTile)tileToCreate;
                LevelManager.I.tilemapCustoms.SetTile(custom.position, custom);
                LevelManager.I.AddToCustomsList(custom);
                break;

            default:
                if (LevelManager.I.tilemapObjects.GetTile<GameTile>(position)) return false;
                LevelManager.I.tilemapObjects.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.I.AddToObjectList(tileToCreate);
                break;
        }

        return true;
    }

    internal override void Undo()
    {
        new EditorDelete(position).Execute();
    }
}
