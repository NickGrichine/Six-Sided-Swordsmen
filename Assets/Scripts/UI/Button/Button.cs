using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Button : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum BUTTON_STATE { ACTIVE, INACTIVE }

    [SerializeField] protected TextMeshProUGUI Text;
    [SerializeField] protected string HoverText;
    public BUTTON_STATE State { get; private set; }
    public Action<Button> onClick;
    public Action<Button> onHoverEnter;
    public Action<Button> onHoverExit;

    void Awake() { }

    public void ClearActions()
    {
        onClick = null;
        onHoverEnter = null;
        onHoverExit = null;
    }

    public void SetState(BUTTON_STATE state)
    {
        State = state;
    }

    public void SetText(string new_text)
    {
        if (Text)
            Text.text = new_text;
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        print("Button was clicked: " + gameObject.name);
        StartCoroutine(DelayedClickCoroutine());
    }

    private IEnumerator DelayedClickCoroutine()
    {
        yield return null;
        onClick?.Invoke(this);
        // SetText("Clicked");
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        onHoverExit?.Invoke(this);
        Debug.Log("Exit on " + this);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        onHoverEnter?.Invoke(this);
        Debug.Log("Hover on " + this);
    }

}

