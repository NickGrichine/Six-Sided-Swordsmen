using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassTurnWindow : MonoBehaviour, IPopupWindow
{
    [SerializeField] private bool enableCameraWhenClosed = true;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI turnPlayerText;
    [SerializeField] private bool useForSetupPhase = false;


    public void Initialize()
    {
        InitializeTurnPlayerText();
        closeButton.onClick += HandleCloseButtonClick;
    }

    private void HandleCloseButtonClick(Button _)
    {
        if (enableCameraWhenClosed && CameraController.Instance)
            CameraController.Instance.EnableCamera();
        Destroy(this.gameObject);
    }

    private void InitializeTurnPlayerText()
    {
        IEnumerator DelayedStart()
        {
            yield return null;
            Player currentPlayer = useForSetupPhase ? SetupManager.Instance.CurrentPlayer : GameManager.Instance.TurnPlayer;
            turnPlayerText.text = $"Player {(int)currentPlayer}'s Turn";
        }
        ;

        StartCoroutine(DelayedStart());
    }
}
