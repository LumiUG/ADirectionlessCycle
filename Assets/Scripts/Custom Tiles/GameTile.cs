using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameTile : TileBase
{
    // Tilebase defaults //
    public Sprite tileSprite;
    public GameObject tileObject;

    // Tile default properties //
    public enum ObjectTypes { Box }
    public Vector3Int position = new();
    public Directions directions = new();

    // Sets the default tile data
    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        // Default tilebase rendering
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;

        // Find object's custom properties references
        // directions.voided = tileObject.transform.Find("Voided").GetComponent<SpriteRenderer>();
        directions.allDir = tileObject.transform.Find("AllDirection").GetComponent<SpriteRenderer>();
        directions.upDir = tileObject.transform.Find("UpDirection").GetComponent<SpriteRenderer>();
        directions.downDir = tileObject.transform.Find("DownDirection").GetComponent<SpriteRenderer>();
        directions.leftDir = tileObject.transform.Find("LeftDirection").GetComponent<SpriteRenderer>();
        directions.rightDir = tileObject.transform.Find("RightDirection").GetComponent<SpriteRenderer>();

        // Updates the sprites for the first time
        directions.UpdateSprites();
    }

    // Returns the tile type.
    public abstract ObjectTypes GetTileType();

    // Adds itself to the level manager's level list (won't work)
    private IEnumerator NotifyLevelManager()
    {
        while (!LevelManager.Instance) { yield return new WaitForSecondsRealtime(0.1f); }

        // Pings the level manager with its own object for addition to the object list.
        LevelManager.Instance.AddToObjectList(this);
        Debug.Log("added");
    }

    // Tile's avaliable directions
    public class Directions
    {
        // Tile values //
        public bool up;
        public bool down;
        public bool left;
        public bool right;

        // Direction tile sprite references //
        public SpriteRenderer voided;
        public SpriteRenderer allDir;
        public SpriteRenderer upDir;
        public SpriteRenderer downDir;
        public SpriteRenderer leftDir;
        public SpriteRenderer rightDir;

        // Constructors //
        public Directions(bool upMovement = true, bool downMovement = true, bool leftMovement = true, bool rightMovement = true)
        {
            up = upMovement;
            down = downMovement;
            left = leftMovement;
            right = rightMovement;
        }

        // Returns if horizontal movement is available
        public bool CanMoveHorizontal() { return left && right; }

        // Returns if vertical movement is available
        public bool CanMoveVertical() { return up && down; }

        // Updates direction sprites
        public void UpdateSprites()
        {
            allDir.gameObject.SetActive(up && down && left && right);
            // voided.gameObject.SetActive(!(up && down && left && right));
            upDir.gameObject.SetActive(up);
            downDir.gameObject.SetActive(down);
            leftDir.gameObject.SetActive(left);
            rightDir.gameObject.SetActive(right);
        }
    }
}
