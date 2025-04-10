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
        GameTile tileToCreate = LevelManager.Instance.CreateTile(tile.ToString(), new(), position);

        // Sets the tile
        switch (tile)
        {
            case ObjectTypes t when LevelManager.Instance.typesSolidsList.Contains(t):
                if (LevelManager.Instance.tilemapCollideable.GetTile<GameTile>(position)) return false;
                LevelManager.Instance.tilemapCollideable.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToCollideableList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesAreas.Contains(t):
                if (LevelManager.Instance.tilemapWinAreas.GetTile<GameTile>(position)) return false;
                LevelManager.Instance.tilemapWinAreas.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToWinAreasList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesHazardsList.Contains(t):
                if (LevelManager.Instance.tilemapHazards.GetTile<GameTile>(position)) return false;
                LevelManager.Instance.tilemapHazards.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToHazardsList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesEffectsList.Contains(t):
                if (LevelManager.Instance.tilemapEffects.GetTile<GameTile>(position)) return false;
                LevelManager.Instance.tilemapEffects.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToEffectsList(tileToCreate);
                break;

            case ObjectTypes t when LevelManager.Instance.typesCustomsList.Contains(t):
                if (LevelManager.Instance.tilemapCustoms.GetTile<CustomTile>(position)) return false;
                CustomTile custom = (CustomTile)tileToCreate;
                LevelManager.Instance.tilemapCustoms.SetTile(custom.position, custom);
                LevelManager.Instance.AddToCustomsList(custom);
                break;

            default:
                if (LevelManager.Instance.tilemapObjects.GetTile<GameTile>(position)) return false;
                LevelManager.Instance.tilemapObjects.SetTile(tileToCreate.position, tileToCreate);
                LevelManager.Instance.AddToObjectList(tileToCreate);
                break;
        }

        return true;
    }

    internal override void Undo()
    {
        new EditorDelete(position).Execute();
    }
}
