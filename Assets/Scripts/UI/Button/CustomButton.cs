using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// TODO:
public class CustomButton : Button
{
    public IButtonDisplayable displayedObject { get; private set; }
    [SerializeField] private Image buttonImage;

    void Awake() { }

    public new void SetState(BUTTON_STATE state)
    {
        base.SetState(state);
        if (state == BUTTON_STATE.INACTIVE)
            DisableRendering();
        else if (state == BUTTON_STATE.ACTIVE)
            EnableRendering();
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
            Text.text = displayedObject.TextDescription;

        // Set icon.
        if (buttonImage)
            buttonImage.sprite = displayedObject.Icon;
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
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        PopupHandler.Instance.Hide();
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
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

    public void ChangeIconColor(Color32 color)
    {
        buttonImage.color = color;
    }
}

