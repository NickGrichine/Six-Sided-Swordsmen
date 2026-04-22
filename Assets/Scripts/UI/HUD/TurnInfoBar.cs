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
    [SerializeField] private Image playerTurnIndicatorBackground;

    void Start()
    {
        turnCounter.text = "Turn _";
        playerTurnIndicator.text = "Player _";
        if (GameManager.Instance)
        {
            GameManager.Instance.OnTurnStart += Initialize;
            GameManager.Instance.OnTurnEnd += Initialize;
        }
        Initialize(GameManager.Instance.TurnNumber);
    }

    private void Initialize(int turnNumber)
    {
        turnCounter.text = "Turn " + turnNumber;
        Player turnPlayer = GameManager.Instance.TurnPlayer;
        switch (turnPlayer)
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

        playerTurnIndicatorBackground.color = Colours.GetColor(turnPlayer);
    }

}
