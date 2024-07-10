public abstract class CustomTile : GameTile
{
    public string customText = "";

    // Tile's effect
    public abstract void Effect(GameTile tile);
}
