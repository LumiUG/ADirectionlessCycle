using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CracksMover : MonoBehaviour
{
    public Mask parentMask;
    public List<RectTransform> masks;
    [Range(-10000f, 10000f)] public float speed = 100.0f;

    void Update()
    {
        if (!parentMask.isActiveAndEnabled) return;
        parentMask.showMaskGraphic = !GameManager.save.preferences.scanlineAnimation;

        if (!GameManager.save.preferences.scanlineAnimation) return;

        // (this code is horrible look away)
        if (speed > 0) masks.FindAll(mask => mask.anchoredPosition.x <= -1180f).ForEach(mask => mask.anchoredPosition = new(1180, mask.anchoredPosition.y));
        else masks.FindAll(mask => mask.anchoredPosition.x >= 1180f).ForEach(mask => mask.anchoredPosition = new(-1180, mask.anchoredPosition.y));
        
        foreach (RectTransform mask in masks) mask.anchoredPosition = new(mask.anchoredPosition.x + speed * Time.deltaTime * -1, mask.anchoredPosition.y);
    }
}
