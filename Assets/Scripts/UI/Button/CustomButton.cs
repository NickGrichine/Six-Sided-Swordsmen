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
    public ButtonDisplayable displayedObject { get; private set; }
    [SerializeField] private Image buttonImage;

    void Awake()
    {
        buttonImage = GetComponent<Image>();
    }

    public void Initialize(ButtonDisplayable displayedObject)
    {
        if (displayedObject == null)
        {
            SetState(BUTTON_STATE.INACTIVE);
            return;
        }

        this.displayedObject = displayedObject;

        // Set text desc.
        if (Text)
            Text.text = displayedObject.GetTextDesc();

        // Set icon.
        if (buttonImage)
            buttonImage.sprite = displayedObject.GetIcon();

        SetState(BUTTON_STATE.ACTIVE);

        // TODO: popup handler.
    }


    private IEnumerator delayedClick()
    {
        yield return null;
        onClick?.Invoke(this);
        Debug.Log("Clicked");
    }

    public new void OnPointerExit(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        // TODO:
        Debug.Log("Exit");
    }

    public new void OnPointerEnter(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        // TODO:
        Debug.Log("Hover");
    }
}

