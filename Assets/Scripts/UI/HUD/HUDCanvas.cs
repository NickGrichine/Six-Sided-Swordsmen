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
    [SerializeField] private CustomButton quitButton;

    void Start()
    {
        // Initialize PassTurnButton:
        if (GameManager.Instance) passTurnButton.onClick += EndTurn;
        passTurnButton.onClick += (button) =>
        {
            ClearUnitConsole();
            UpdateUnitConsole();
        };
        passTurnButton.SetText("Pass Turn");
        passTurnButton.SetState(Button.BUTTON_STATE.ACTIVE);

        // Initialize Quit button:
        quitButton.onClick += LoadTitleScene;
        quitButton.SetState(Button.BUTTON_STATE.ACTIVE);

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        SubscribeAllHealthManagersToUnitConsole();
    }

    private void LoadTitleScene(Button _)
    {
        SceneLoader.Instance.LoadScene("Title");
    }

    private void SubscribeAllHealthManagersToUnitConsole()
    {
        HealthManager[] allHealthManagers = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsSortMode.None);
        foreach (HealthManager manager in allHealthManagers)
        {
            manager.onDamage += (int i) => { UpdateUnitConsole(); };
            manager.onPermanentDamage += (int i) => { UpdateUnitConsole(); };
            manager.onCrit += (int i) => { UpdateUnitConsole(); };
            manager.onDodge += () => { UpdateUnitConsole(); };
            manager.onHeal += (int i) => { UpdateUnitConsole(); };
            manager.onDeath += () => { UpdateUnitConsole(); };
        }
    }

    private void ClearUnitConsole() => UnitConsole.Instance.ClearUnitConsole();
    private void UpdateUnitConsole()
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
