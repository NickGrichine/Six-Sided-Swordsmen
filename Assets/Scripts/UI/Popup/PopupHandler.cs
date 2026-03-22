using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PopupHandler : Singleton<PopupHandler>
{
    [SerializeField] private TextMeshProUGUI contentField;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform _rect_transform;

    protected override void Awake()
    {
        base.Awake();
        _rect_transform = GetComponent<RectTransform>();
        Hide();
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        // Calculate Pivot based on screen half
        // If mouse is on the right 50% of the screen, flip pivot to 1 (rightmost of the popup)
        float pivotX = mousePos.x > Screen.width / 2 ? 1f : 0f;

        // Do the same for the bottom half to keep it above the cursor
        float pivotY = mousePos.y < Screen.height / 2 ? 0f : 1f;

        _rect_transform.pivot = new Vector2(pivotX, pivotY);

        // Apply a small "Padding" offset so it doesn't touch the actual cursor pixels
        Vector2 offset = new Vector2(pivotX == 0 ? 20 : 0, pivotY == 1 ? -20 : 20);

        _rect_transform.position = mousePos + offset;
    }

    public void Show(string text)
    {
        if (text.Length == 0) return;
        contentField.text = text;
        canvasGroup.alpha = 1;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
    }
}
