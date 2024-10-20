using UnityEngine;
using UnityEngine.EventSystems;

public class CustomLevels : MonoBehaviour
{
    public RectTransform holder;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(transform.Find("Back Button").gameObject);
    }

    // void Update()
    // {
    //     rt.anchoredPosition += Time.deltaTime * (Vector2.up * 22f);  
    // }
}
