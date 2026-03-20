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
    public BUTTON_STATE State { get; private set; }
    public Action<Button> onClick;
    public Action<Button> onHover;

    void Awake()
    {
        // Text = GetComponent<TextMeshProUGUI>();
    }

    public void ClearActions()
    {
        onClick = null;
        onHover = null;
    }

    public void SetState(BUTTON_STATE state)
    {
        State = state;
    }

    public void SetText(string new_text)
    {
        if (Text == null) return;
        Text.text = new_text;
    }

    public void OnPointerDown(PointerEventData eventData)
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

    public void OnPointerExit(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        // TODO:
        // SetText("Exit");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (State == BUTTON_STATE.INACTIVE) return;
        // TODO:
        // SetText("Hover");
    }

}

