using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Selectors : MonoBehaviour
{
    internal RectTransform left;
    internal RectTransform right;

    private GameObject tracking = null;
    private RectTransform trackRT = null;
    private Vector2 distanceRight = Vector2.zero;
    private Vector2 distanceLeft = Vector2.zero;
    private bool stopMoving = false;

    void Start()
    {
        if (UI.Instance) UI.Instance.selectors = this;
        left = transform.Find("Left").gameObject.GetComponent<RectTransform>();
        right = transform.Find("Right").gameObject.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null) return;

        // Slowly move towards the target position
        if (tracking != null) MoveSelector();

        // New object?
        if (tracking == EventSystem.current.currentSelectedGameObject) return;

        // Get the new reference
        tracking = EventSystem.current.currentSelectedGameObject;
        trackRT = tracking.GetComponent<RectTransform>();
        stopMoving = false;

        // Move the selectors to the currently selected UI object
        distanceRight = trackRT.rect.center + new Vector2(trackRT.rect.width + (right.rect.width / 2), 0);
        distanceLeft = trackRT.rect.center + new Vector2(-trackRT.rect.width - (left.rect.width / 2), 0);

        right.SetParent(trackRT);
        left.SetParent(trackRT);
        right.localScale = Vector3.one;
        left.localScale = Vector3.one * -1;
    }

    // Moves the selector to a target direction
    private void MoveSelector()
    {
        if (stopMoving) return;

        float rightSpeed = Vector2.Distance(right.anchoredPosition, distanceRight) * Time.deltaTime * 10f;
        float leftSpeed = Vector2.Distance(left.anchoredPosition, distanceLeft) *  Time.deltaTime * 10f;
        if (rightSpeed <= 0.001f && leftSpeed <= 0.001f) stopMoving = true;

        right.anchoredPosition = Vector2.MoveTowards(right.anchoredPosition, distanceRight, rightSpeed);
        left.anchoredPosition = Vector2.MoveTowards(left.anchoredPosition, distanceLeft, leftSpeed);
    }
}
