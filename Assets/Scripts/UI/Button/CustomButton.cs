using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button
{
    public IButtonDisplayable displayedObject { get; private set; }
    [SerializeField] private Image buttonImage;
    [SerializeField] private bool supressPopup = false;
    [SerializeField] private float highlightIntensity = 0.1f;

    private bool useDimming = false;
    private Color baseColor;
    private Color highlightColor;
    private Color clickColor;
    private bool hovered = false;

    void Awake()
    {
        useDimming = highlightIntensity > 0.0f;
        if (!buttonImage)
            buttonImage = GetComponent<Image>();
        ChangeIconColor(buttonImage.color);
        if (useDimming)
            EnableHoverHighlighting();
    }


    public new void SetState(BUTTON_STATE state)
    {
        base.SetState(state);
        if (state == BUTTON_STATE.INACTIVE)
            DisableRendering();
        else if (state == BUTTON_STATE.ACTIVE)
            EnableRendering();
    }

    private void EnableHoverHighlighting()
    {
        onHoverEnter += (_) =>
        {
            hovered = true;
            UseHighlightColor();
        };
        onHoverExit += (_) =>
        {
            hovered = false;
            UseBaseColor();
        };
    }
    private void UseHighlightColor()
    {
        buttonImage.color = highlightColor;
        RedrawColor();
    }
    private void UseBaseColor()
    {
        buttonImage.color = baseColor;
        RedrawColor();
    }
    private void RedrawColor()
    {
        if (!useDimming)
        {
            buttonImage.color = highlightColor;
            return;
        }
        if (hovered)
            buttonImage.color = highlightColor;
        else
            buttonImage.color = baseColor;
    }

    public void Initialize(IButtonDisplayable displayedObject)
    {
        if (displayedObject == null)
        {
            SetState(BUTTON_STATE.INACTIVE);
            return;
        }

        SetState(BUTTON_STATE.ACTIVE);
        this.displayedObject = displayedObject;

        // Set text desc.
        if (Text)
            Text.text = displayedObject.GetTextDescription();

        // Set icon.
        if (buttonImage && displayedObject.GetIcon())
            buttonImage.sprite = displayedObject.GetIcon();

        if (useDimming)
            EnableHoverHighlighting();
    }

    public void Initialize(Sprite sprite)
    {
        if (!sprite)
        {
            SetState(BUTTON_STATE.INACTIVE);
            return;
        }
        SetState(BUTTON_STATE.ACTIVE);
        if (buttonImage)
            buttonImage.sprite = sprite;

        if (useDimming)
            EnableHoverHighlighting();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (State == BUTTON_STATE.INACTIVE) return;
        if (!supressPopup)
            PopupHandler.Instance.Hide();
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (State == BUTTON_STATE.INACTIVE) return;
        if (!supressPopup)
            PopupHandler.Instance.Show(HoverText);
    }

    private void DisableRendering()
    {
        if (buttonImage) buttonImage.enabled = false;
    }
    private void EnableRendering()
    {
        if (buttonImage) buttonImage.enabled = true;
    }

    public void ClearIcon()
    {
        if (buttonImage)
        {
            buttonImage.sprite = null;
        }
    }

    public void ChangeIconColor(Color color)
    {
        if (!buttonImage) return;
        baseColor = color * (1f - highlightIntensity);
        highlightColor = color;
        RedrawColor();
    }
}

