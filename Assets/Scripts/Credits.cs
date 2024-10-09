using UnityEngine;
using UnityEngine.EventSystems;

public class Credits : MonoBehaviour
{
    private RectTransform rt;

    void Start() { rt = transform.Find("Holder").GetComponent<RectTransform>(); EventSystem.current.SetSelectedGameObject(transform.Find("Back Button").gameObject); }

    // void Update()
    // {
    //     rt.anchoredPosition += Time.deltaTime * (Vector2.up * 22f);  
    // }

    // Moves credits down
    public void Down()
    {
        if (rt.anchoredPosition.y >= 1080 * 1) return; // number of screens -1
        rt.anchoredPosition += new Vector2(0, 1080);
    }

    // Moves credits up
    public void Up()
    {
        if (rt.anchoredPosition.y <= 0) return; 
        rt.anchoredPosition -= new Vector2(0, 1080);
    }
}
