using UnityEngine;

public class Float : MonoBehaviour
{
    [Range(-1000f, 1000f)] public float amountX;
    [Range(-1000f, 1000f)] public float amountY = 10f;
    [Range(0, 1f)] public int dirX = 0;
    [Range(0, 1f)] public int dirY = 1;
    [Range(0.1f, 10f)] public float speed = 1f;

    private Vector2 originalPos;
    private RectTransform anchor;
    private bool direction = true;

    private void Awake()
    {
        anchor = transform as RectTransform;
        originalPos = anchor.anchoredPosition;
    }

    private void FixedUpdate()
    {
        Vector2 addition;
        if (direction) addition = speed *  new Vector2(dirX, dirY);
        else addition = speed * new Vector2(-dirX, -dirY);

        // Maximums (top)
        if (direction && (anchor.anchoredPosition + addition).x > originalPos.x + amountX) {
            anchor.anchoredPosition = new Vector2(originalPos.x + amountX, anchor.anchoredPosition.y);
            direction = false;
        } else if (direction && (anchor.anchoredPosition + addition).y > originalPos.y + amountY) {
            anchor.anchoredPosition = new Vector2(anchor.anchoredPosition.x, originalPos.y + amountY);
            direction = false;
        }

        // Maximums (bottom)
        if (!direction && (anchor.anchoredPosition + addition).x < originalPos.x - amountX) {
            anchor.anchoredPosition = new Vector2(originalPos.x - amountX, anchor.anchoredPosition.y);
            direction = true;
        } else if (!direction && (anchor.anchoredPosition + addition).y < originalPos.y - amountY) {
            anchor.anchoredPosition = new Vector2(anchor.anchoredPosition.x, originalPos.y - amountY);
            direction = true;
        }

        // Move
        anchor.anchoredPosition += addition;
    }
}