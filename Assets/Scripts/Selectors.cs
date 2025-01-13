using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Selectors : MonoBehaviour
{
    [HideInInspector] public bool instant; 
    internal RectTransform left;
    internal RectTransform right;
    internal Image leftImage;
    internal Image rightImage;

    private GameObject tracking = null;
    private RectTransform trackRT = null;
    private Vector2 distanceRight = Vector2.zero;
    private Vector2 distanceLeft = Vector2.zero;
    private bool stopMoving = false;
    private Color visibleColor = new(1, 1, 1, 1);

    void Start()
    {
        if (UI.Instance) UI.Instance.selectors = this;
        GetSelectors();
        instant = false;
    }

    void Update()
    {
        if (EventSystem.current)
        {
            if (EventSystem.current.currentSelectedGameObject == null) return;
        }

        // Null check, recreate selectors upon destroying
        if (!right || !left)
        {
            Instantiate(Resources.Load("Prefabs/Selectors/Left"), transform).name = "Left";
            Instantiate(Resources.Load("Prefabs/Selectors/Right"), transform).name = "Right";
            instant = true; // using this so it automatically sets to its destination
            GetSelectors();
        }

        // Is its image disabled? (dunno why this happens!)
        if (!rightImage.enabled || !leftImage.enabled) 
        {
            rightImage.enabled = true;
            leftImage.enabled = true;
        }

        // Slowly move towards the target position
        if (tracking != null) MoveSelector();

        // New object?
        if (!EventSystem.current) return;
        leftImage.color = visibleColor;
        rightImage.color = visibleColor;

        if (tracking == EventSystem.current.currentSelectedGameObject) return;

        // Select SFX
        // if (!instant) AudioManager.Instance.PlaySFX(AudioManager.select, 0.20f, true);

        // Get the new reference
        tracking = EventSystem.current.currentSelectedGameObject;
        trackRT = tracking.GetComponent<RectTransform>();
        stopMoving = false;

        // Move the selectors to the currently selected UI object
        SetSelector(trackRT, instant);
        instant = false;
    }

    // Sets the selector to a RectTransform object
    internal void SetSelector(RectTransform rt, bool forceMove = false)
    {
        if (rt == null || !right || !left) return;

        distanceRight = rt.rect.center + new Vector2(rt.rect.width + (right.rect.width / 2), 0);
        distanceLeft = rt.rect.center + new Vector2(-rt.rect.width - (left.rect.width / 2), 0);

        right.SetParent(rt);
        left.SetParent(rt);
        right.localScale = new(1, 1, 1);
        left.localScale = new(-1, 1, 1);
        left.rotation = Quaternion.identity;
        right.rotation = Quaternion.identity;

        // (optionally) sets the position to the RectTransform
        if (forceMove)
        {
            right.anchoredPosition = distanceRight;
            left.anchoredPosition = distanceLeft;
        }
    }

    //
    internal void ChangeSelected(GameObject obj, bool instant = false) 
    {
        if (obj == null) return;

        this.instant = instant;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(obj);
        if (instant) SetSelector(obj.GetComponent<RectTransform>(), instant);
        this.instant = false;
    }

    // Moves the selector to a target direction
    private void MoveSelector()
    {
        if (stopMoving) return;

        float rightSpeed = Vector2.Distance(right.anchoredPosition, distanceRight) * Time.deltaTime * 10f;
        float leftSpeed = Vector2.Distance(left.anchoredPosition, distanceLeft) * Time.deltaTime * 10f;
        if (rightSpeed <= 0.001f && leftSpeed <= 0.001f) stopMoving = true;

        right.anchoredPosition = Vector2.MoveTowards(right.anchoredPosition, distanceRight, rightSpeed);
        left.anchoredPosition = Vector2.MoveTowards(left.anchoredPosition, distanceLeft, leftSpeed);
        left.rotation = Quaternion.identity;
        right.rotation = Quaternion.identity;
    }

    // Gets the references of the selectors
    private void GetSelectors()
    {
        left = transform.Find("Left").gameObject.GetComponent<RectTransform>();
        right = transform.Find("Right").gameObject.GetComponent<RectTransform>();
        leftImage = left.GetComponent<Image>();
        rightImage = right.GetComponent<Image>();
    }
}
