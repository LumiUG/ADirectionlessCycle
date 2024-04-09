using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameTile : TileBase
{
    // Tilebase defaults //
    public Sprite tileSprite;
    public GameObject tileObject;

    // Tile default properties //
    public enum ObjectTypes { Box, Area, Circle, Hexahedron }
    public Vector3Int position = new();
    public Directions directions = new(true, true, true, true);

    // Sets the default tile data
    public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
    {
        // Default tilebase rendering
        tileData.sprite = tileSprite;
        tileData.gameObject = tileObject;

        // Adds itself to the level manager's level objects/areas/etc list
        if (!LevelManager.Instance) return;
        LevelManager.Instance.AddToObjectList(this);
        LevelManager.Instance.AddToAreaList(this);

        // Find object's custom properties references
        if (!tileData.gameObject) return;
        directions.GetSpriteReferences(tileObject);

        // Updates the sprites for the first time
        directions.UpdateSprites();
    }

    // Returns the tile type.
    public abstract ObjectTypes GetTileType();

    // DEFAULT // Checks colisions between collideables and objects // DEFAULT //
    public virtual Vector3Int CollisionHandler(Vector3Int checkPosition, Vector3Int direction, Tilemap tilemapObjects, Tilemap tilemapCollideable)
    {
        // Get the collissions
        GameTile objectCollidedWith = tilemapObjects.GetTile<GameTile>(checkPosition);
        bool collideableCollision = tilemapCollideable.GetTile(checkPosition) != null;
        bool objectCollision = objectCollidedWith != null;

        // Checks if it is able to move
        if (collideableCollision || (objectCollision && !objectCollidedWith.directions.pushable)) return Vector3Int.back;

        // "Pushes" objects infront. Recursion!
        else if (objectCollision) if (!LevelManager.Instance.TryMove(checkPosition, checkPosition + direction, direction, true)) return Vector3Int.back;
        return checkPosition;
    }

    // Tile's avaliable directions
    public class Directions
    {
        // Tile values //
        public bool pushable = true; // Slight bugs with this property!
        public bool up;
        public bool down;
        public bool left;
        public bool right;

        // Direction tile sprite references //
        private SpriteRenderer voided;
        private SpriteRenderer pushableSprite;
        private SpriteRenderer allDir;
        private SpriteRenderer upDir;
        private SpriteRenderer downDir;
        private SpriteRenderer leftDir;
        private SpriteRenderer rightDir;

        // Constructors //
        public Directions(bool upMovement = true, bool downMovement = true, bool leftMovement = true, bool rightMovement = true)
        {
            // We don't call for SetNewDirection's UpdateSprites as we have no references.
            SetNewDirections(upMovement, downMovement, leftMovement, rightMovement);
        }

        // Sets new directions for the tile
        public void SetNewDirections(bool upMovement = true, bool downMovement = true, bool leftMovement = true, bool rightMovement = true)
        {
            up = upMovement;
            down = downMovement;
            left = leftMovement;
            right = rightMovement;

            // Update sprites if at least one object reference is set
            if (allDir) UpdateSprites();
        }

        // Returns if horizontal movement is available
        public bool CanMoveHorizontal() { return left && right; }

        // Returns if vertical movement is available
        public bool CanMoveVertical() { return up && down; }
        
        // Sets private sprites
        public void GetSpriteReferences(GameObject parent)
        {
            if (!parent) return;

            // directions.voided = tileObject.transform.Find("Voided").GetComponent<SpriteRenderer>();
            allDir = parent.transform.Find("AllDirection").GetComponent<SpriteRenderer>();
            // directions.pushableSprite = tileObject.transform.Find("Pushable").GetComponent<SpriteRenderer>();
            upDir = parent.transform.Find("UpDirection").GetComponent<SpriteRenderer>();
            downDir = parent.transform.Find("DownDirection").GetComponent<SpriteRenderer>();
            leftDir = parent.transform.Find("LeftDirection").GetComponent<SpriteRenderer>();
            rightDir = parent.transform.Find("RightDirection").GetComponent<SpriteRenderer>();
        }

        // Updates direction sprites
        public void UpdateSprites()
        {
            // voided.gameObject.SetActive(!(up && down && left && right));
            allDir.gameObject.SetActive(up && down && left && right);
            // voided.gameObject.SetActive(canBePushed);
            upDir.gameObject.SetActive(up && !(up && down && left && right));
            downDir.gameObject.SetActive(down && !(up && down && left && right));
            leftDir.gameObject.SetActive(left && !(up && down && left && right));
            rightDir.gameObject.SetActive(right && !(up && down && left && right));
        }
    }
}
