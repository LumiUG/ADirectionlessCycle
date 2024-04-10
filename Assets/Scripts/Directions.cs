using UnityEngine;

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