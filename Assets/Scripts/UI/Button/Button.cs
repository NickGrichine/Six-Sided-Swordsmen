using System.Collections.Generic;
using UnityEngine;
using TMPro;

public abstract class Button : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum BUTTON_STATE { ACTIVE, INACTIVE }

    [SerializeField] protected TextMeshProUGUI Text;
    public BUTTON_STATE State { get; private set; }
    public Action<Button> OnClick;
    public Action<Button> OnHover;



    public void SetState(BUTTON_STATE state)
    {
        State = state;
    }


    public void SetText(string new_text)
    {
        Text.text = new_text;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (state == BUTTON_STATE.INACTIVE) return;
        StartCoroutine(delayedClick());
    }

    private IEnumerator delayedClick()
    {
        yield return null;
        onClick?.Invoke(this);
        // clickFeedback();
    }


}

