using System;
using UnityEngine;

// Tile's avaliable directions
[Serializable]
public class Directions
{
    // Tile values //
    public bool pushable = true;
    public bool up;
    public bool down;
    public bool left;
    public bool right;

    // Direction tile sprite references //
    private SpriteRenderer pushableSprite;
    private SpriteRenderer upDir;
    private SpriteRenderer downDir;
    private SpriteRenderer leftDir;
    private SpriteRenderer rightDir;

    // Editor //
    [NonSerialized] internal bool editorDirections;
    [NonSerialized] internal bool editorPushable;
    [NonSerialized] internal int editorMinimumDirections;

    // Constructors //
    public Directions(bool upMovement = true, bool downMovement = true, bool leftMovement = true, bool rightMovement = true, bool pushableMovement = true)
    {
        pushable = pushableMovement;
        
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
        if (upDir) UpdateSprites();
    }

    // Returns if horizontal movement is available
    public bool CanMoveHorizontal() { return left && right; }

    // Returns if vertical movement is available
    public bool CanMoveVertical() { return up && down; }

    // Sets private sprites
    public void GetSpriteReferences(GameObject parent)
    {
        parent.TryGetComponent(out SpriteRenderer componentCheck);
        if (!parent || componentCheck == null || parent.name == "PingDrawOver") return;

        pushableSprite = parent.transform.Find("Pushable").GetComponent<SpriteRenderer>();
        upDir = parent.transform.Find("UpDirection").GetComponent<SpriteRenderer>();
        downDir = parent.transform.Find("DownDirection").GetComponent<SpriteRenderer>();
        leftDir = parent.transform.Find("LeftDirection").GetComponent<SpriteRenderer>();
        rightDir = parent.transform.Find("RightDirection").GetComponent<SpriteRenderer>();
    }

    // Updates direction sprites
    public void UpdateSprites()
    {
        if (!upDir) return;

        pushableSprite.gameObject.SetActive(!pushable);
        upDir.gameObject.SetActive(up);
        downDir.gameObject.SetActive(down);
        leftDir.gameObject.SetActive(left);
        rightDir.gameObject.SetActive(right);
    }

    // Returns the amount of directions
    public int GetActiveDirectionCount()
    {
        return Convert.ToInt32(up) + Convert.ToInt32(down) + Convert.ToInt32(left) + Convert.ToInt32(right);
    }
}