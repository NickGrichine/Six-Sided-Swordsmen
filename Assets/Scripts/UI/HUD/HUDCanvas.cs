using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUDCanvas : Singleton<HUDCanvas>
{
    [SerializeField] private CustomButton passTurnButton;

    void Start()
    {
        // Initialize PassTurnButton:
        if (GameManager.Instance) passTurnButton.onClick += EndTurn;
        passTurnButton.onClick += UpdateUnitConsole;
        passTurnButton.SetText("Pass Turn");
        passTurnButton.SetState(Button.BUTTON_STATE.ACTIVE);
    }

    private void UpdateUnitConsole(Button button)
    {
        Tile selected_tile = GridEventHandler.Instance.SelectedTile;
        UnitConsole.Instance.UpdateUnitConsole(selected_tile);
    }

    private void EndTurn(Button button)
    {
        GameManager.Instance.EndTurn();
        Debug.Log("Pass the turn");
    }
}
