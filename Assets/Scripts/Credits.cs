using UnityEngine;

public class Credits : MonoBehaviour
{
    private RectTransform rt;

    void Start()
    {
        UI.Instance.selectors.ChangeSelected(transform.Find("Back Button").gameObject, true);
        rt = transform.Find("Holder").GetComponent<RectTransform>();
    }

    // void Update()
    // {
    //     // rt.anchoredPosition += Time.deltaTime * (Vector2.up * 22f);
    // }

    // Moves credits right
    public void Right()
    {
        if (rt.anchoredPosition.x <= -1920 * 4) return;
        rt.anchoredPosition -= new Vector2(1920, 0);
    }

    // Moves credits left
    public void Left()
    {
        if (rt.anchoredPosition.x == 0) return;
        rt.anchoredPosition += new Vector2(1920, 0);
    }
}
