using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Selectors : MonoBehaviour
{
    internal RectTransform left = null;
    internal RectTransform right = null;
    private GameObject tracking = null;

    void Start()
    {
        if (UI.Instance) UI.Instance.selectors = this;
        left = transform.Find("Left").gameObject.GetComponent<RectTransform>();
        right = transform.Find("Right").gameObject.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null) return;
        else if (tracking == EventSystem.current.currentSelectedGameObject) return;

        // Get the new reference
        tracking = EventSystem.current.currentSelectedGameObject;
        RectTransform rt = tracking.GetComponent<RectTransform>();
        
        // Debug.Log($"{rt.position.x}, {rt.rect.width}");

        // Move the selectors to the currently selected UI object
        right.SetParent(rt);
        left.SetParent(rt);
        right.anchoredPosition = rt.rect.center + new Vector2(rt.rect.width + (right.rect.width / 2), 0);
        left.anchoredPosition = rt.rect.center + new Vector2(-rt.rect.width - (left.rect.width / 2), 0);
        right.localScale = Vector3.one;
        left.localScale = Vector3.one * -1;
    }
}
