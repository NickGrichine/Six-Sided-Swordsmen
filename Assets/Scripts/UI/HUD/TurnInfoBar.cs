using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TurnInfoBar : Singleton<TurnInfoBar>
{
    [SerializeField] private TextMeshProUGUI turnCounter;
    [SerializeField] private TextMeshProUGUI playerTurnIndicator;

    void Start()
    {
        turnCounter.text = "Turn _";
        playerTurnIndicator.text = "Player _";
        if (GameManager.Instance)
        {
            GameManager.Instance.OnTurnStart += Initialize;
            GameManager.Instance.OnTurnEnd += Initialize;
        }
    }

    private void Initialize(int turnNumber)
    {
        turnCounter.text = "Turn " + turnNumber;
        switch (GameManager.Instance.TurnPlayer)
        {
            case Player.PLAYER_1:
                playerTurnIndicator.text = "Player 1";
                break;
            case Player.PLAYER_2:
                playerTurnIndicator.text = "Player 2";
                break;
            default:
                break;
        }
    }

}
