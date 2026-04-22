using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReplaySummaryWindow : MonoBehaviour, IPopupWindow
{
    [SerializeField] private bool enableCameraWhenClosed = true;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI turnPlayerText;
    [SerializeField] private TextMeshProUGUI replaySummaryText;


    public Button GetChainButton() => closeButton;

    public void Initialize()
    {
        InitializeTurnPlayerText();
        InitializeReplaySummaryText();
        closeButton.onClick += HandleCloseButtonClick;
    }

    private void HandleCloseButtonClick(Button _)
    {
        if (enableCameraWhenClosed && CameraController.Instance)
            CameraController.Instance.EnableCamera();
        Destroy(this.gameObject);
    }

    private void InitializeReplaySummaryText()
    {
        replaySummaryText.text = ReplayTurnSummary.GetLastEnemyTurnSummary();
    }

    private void InitializeTurnPlayerText()
    {
        IEnumerator DelayedStart()
        {
            yield return null;
            Player currentPlayer = GameManager.Instance.TurnPlayer;
            turnPlayerText.text = $"Player {(int)currentPlayer}'s Turn";
            turnPlayerText.color = Colours.GetColor(currentPlayer);
        }
        ;

        StartCoroutine(DelayedStart());
    }
}
