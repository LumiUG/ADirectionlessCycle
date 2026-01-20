using UnityEngine;

public class SettingsBGMover : MonoBehaviour
{
    public RectTransform g1;
    public RectTransform g2;
    public RectTransform g1Point;
    public RectTransform g2Point;
    [Range(-4f, 4f)] public float speed = 1.0f;

    void Update()
    {
        if (g1.anchoredPosition.x <= -42) g1.anchoredPosition = new(39.9f, g1.anchoredPosition.y);
        if (g2.anchoredPosition.x <= -42) g2.anchoredPosition = new(39.9f, g2.anchoredPosition.y);
        // if (g1.anchoredPosition.x <= -32) g1.anchoredPosition = new(g2.anchoredPosition.x + g2Point.anchoredPosition.x, g1.anchoredPosition.y);
        // if (g2.anchoredPosition.x <= -32) g2.anchoredPosition = new(g1.anchoredPosition.x + g1Point.anchoredPosition.x, g2.anchoredPosition.y);

        g1.anchoredPosition = new(g1.anchoredPosition.x + speed * Time.deltaTime * -1, g1.anchoredPosition.y);
        g2.anchoredPosition = new(g2.anchoredPosition.x + speed * Time.deltaTime * -1, g2.anchoredPosition.y);

        // -37.5 goes out
        // 44.3 goes in
    }
}
