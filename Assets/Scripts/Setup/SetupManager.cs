using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SetupManager : Singleton<SetupManager>
{
    [SerializeField] private Button FinishSetupButton;
    [SerializeField] private PopupWindowManager PassTurnWindowInitializer;

    public Player CurrentPlayer { get; private set; } = Player.PLAYER_1;

    private int _player_length = Enum.GetValues(typeof(Player)).Length;
    private int _player_index = 1;
    private int _save_slot = 0;

    public event Action onTurnPass;
    public event Action onSetupStart;


    void Start()
    {
        FinishSetupButton.onClick += (_) => EndSetupForCurrentPlayer();
        FinishSetupButton.onClick += (_) => ResourceManager.Instance.UpdateResourceDisplay();
        FinishSetupButton.onClick += (_) => UnitDisplay.Instance.ClearUnitDisplay();
        PassTurnWindowInitializer.Initialize();
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        onSetupStart?.Invoke();
    }

    public void EndSetupForCurrentPlayer()
    {
        _player_index++;
        if (_player_index >= _player_length)
        {
            EndSetupPhase();
            return;
        }
        CurrentPlayer = (Player)_player_index;
        onTurnPass?.Invoke();
        Debug.Log("Current player is " + CurrentPlayer);
    }

    public void EndSetupPhase()
    {
        PassTurnWindowInitializer.Disable();
        HexGridManager hex_grid_manager = HexGridManager.Instance;
        if (hex_grid_manager == null)
        {
            Debug.LogError("SetupManager: HexGridManager.Instance not found");
            return;
        }

        CacheManager cacheManager = CacheManager.Instance;
        if (cacheManager == null)
        {
            Debug.LogError("SetupManager: CacheManager.Instance not found");
            return;
        }

        SaveData saveData = new SaveData("Setup Cache", -1, hex_grid_manager, 1, Player.PLAYER_1);
        cacheManager.Write(saveData);

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null)
        {
            Debug.LogError("SetupManager: SceneLoader.Instance not found");
            return;
        }

        sceneLoader.LoadScene("Game Scene");
    }
}


