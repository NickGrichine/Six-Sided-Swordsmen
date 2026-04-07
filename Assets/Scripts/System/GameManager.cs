using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public int TurnNumber { get; private set; }
    public Player TurnPlayer { get; private set; } = Player.NULL;

    public event Action<int> OnTurnStart;   // passes new turn
    public event Action<int> OnTurnEnd;     // passes turn that's ending
    public event Action OnGameStateChanged;

    private bool gameOngoing;


    private void Start()
    {
        // For testing purposes, start the game immediately
        StartGame();
    }

    public void StartGame()
    {
        TurnPlayer = NextPlayer();
        TurnNumber = 1;
        gameOngoing = true;
        OnTurnStart?.Invoke(TurnNumber);
        NotifyGameStateChanged();
    }

    public void EndTurn()
    {
        // create notification for unused actions, etc. and delay calling this function
        // done by some end turn menu/ event handler

        OnTurnEnd?.Invoke(TurnNumber);

        TurnNumber ++;
        TurnPlayer = NextPlayer();

        OnTurnStart?.Invoke(TurnNumber);
        NotifyGameStateChanged();
    }

    public void NotifyGameStateChanged()
    {
        print("Game state change event invoked");
        OnGameStateChanged?.Invoke();
    }
    
    private Player NextPlayer()
    {
        if (TurnPlayer == Player.NULL)
        {
            
        }
        if (TurnPlayer == Player.PLAYER_1)
        {
            return Player.PLAYER_2;
        }
        else
        {
            return Player.PLAYER_1;
        }
    }
}
