using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassTurnWindow : MonoBehaviour, IPopupWindow
{
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI turnPlayerText;


    public void Initialize()
    {
        InitializeTurnPlayerText();
        closeButton.onClick += (_) => Destroy(this.gameObject);
    }


    private void InitializeTurnPlayerText()
    {
        IEnumerator DelayedStart()
        {
            yield return null;
            Player currentPlayer = GameManager.Instance.TurnPlayer;
            turnPlayerText.text = "Player\n" + (int)currentPlayer;
        }
        ;

        StartCoroutine(DelayedStart());
    }
}
