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

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        // ClearIcon();
    }

    public new void SetState(BUTTON_STATE state)
    {
        base.SetState(state);
        if (state == BUTTON_STATE.INACTIVE)
            buttonImage.enabled = false;
        else if (state == BUTTON_STATE.ACTIVE)
            buttonImage.enabled = true;
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
        else
            buttonImage.sprite = null;

        // TODO: popup handler.
    }


    private IEnumerator delayedClick()
    {
        yield return null;
        onClick?.Invoke(this);
        Debug.Log("Clicked on " + this);
    }
    public new void OnPointerExit(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        // TODO:
        Debug.Log("Exit on " + this);
    }
    public new void OnPointerEnter(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        // TODO:
        Debug.Log("Hover on " + this);
    }


    public void ClearIcon()
    {
        IEnumerator delayed_clear()
        {
            yield return null;
            if (buttonImage) buttonImage.sprite = null; // clear icon.
        }
        StartCoroutine(delayed_clear());
    }
}

