using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameObject gameEndWindowPrefab;
    [SerializeField] private Canvas canvasObject;

    public int TurnNumber { get; private set; }
    public Player TurnPlayer { get; private set; } = Player.NULL;

    public event Action<int> OnTurnStart;   // passes new turn
    public event Action<int> OnTurnEnd;     // passes turn that's ending
    public event Action OnGameStateChanged;

    private bool gameOngoing;
    private bool gameEnded;


    private void Start()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.ObserveGameManager(this);
        }

        Invoke(nameof(StartGame), 0.1f);
    }

    private void OnDestroy()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.StopObservingGameManager(this);
        }
    }

    public void StartGame()
    {
        TurnPlayer = NextPlayer();
        TurnNumber = 1;
        gameOngoing = true;
        gameEnded = false;
        ReplayManager.EnsureExists().RecordTurnStarted(TurnNumber, TurnPlayer);
        OnTurnStart?.Invoke(TurnNumber);
        NotifyGameStateChanged();
    }

    public void EndTurn()
    {
        if (gameEnded)
        {
            return;
        }

        // create notification for unused actions, etc. and delay calling this function
        // done by some end turn menu/ event handler

        ReplayManager.EnsureExists().RecordTurnEnded(TurnNumber, TurnPlayer);
        OnTurnEnd?.Invoke(TurnNumber);

        TurnNumber ++;
        TurnPlayer = NextPlayer();

        ReplayManager.EnsureExists().RecordTurnStarted(TurnNumber, TurnPlayer);
        OnTurnStart?.Invoke(TurnNumber);
        NotifyGameStateChanged();
    }

    public void NotifyGameStateChanged()
    {
        print("Game state change event invoked");
        OnGameStateChanged?.Invoke();

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == "Game Scene")
        {
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        if (!gameOngoing || gameEnded)
        {
            return;
        }

        bool player1HasUnits = false;
        bool player2HasUnits = false;

        UnitController[] units = FindObjectsOfType<UnitController>();
        foreach (UnitController unit in units)
        {
            if (unit == null || unit.position == null)
            {
                continue;
            }

            if (unit.teamID == Player.PLAYER_1)
            {
                player1HasUnits = true;
            }
            else if (unit.teamID == Player.PLAYER_2)
            {
                player2HasUnits = true;
            }

            if (player1HasUnits && player2HasUnits)
            {
                return;
            }
        }

        if (player1HasUnits == player2HasUnits)
        {
            return;
        }

        gameEnded = true;
        gameOngoing = false;

        DataManager.Instance?.DeleteActiveGame();

        if (gameEndWindowPrefab == null)
        {
            Debug.LogWarning("GameManager: gameEndWindowPrefab is not assigned.");
            return;
        }

        if (canvasObject == null)
        {
            Debug.LogWarning("GameManager: canvasObject is not assigned.");
            return;
        }

        GameObject windowObject = Instantiate(gameEndWindowPrefab, canvasObject.transform);

        IPopupWindow popupWindow = windowObject.GetComponent<IPopupWindow>();
        popupWindow?.Initialize();
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
