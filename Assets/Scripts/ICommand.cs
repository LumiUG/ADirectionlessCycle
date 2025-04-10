using UnityEngine;
using static GameTile;

public abstract class ICommand
{
    public ObjectTypes tile;
    public Vector3Int position;
    public string customString;

    internal abstract bool Execute();
    internal abstract void Undo();
}
