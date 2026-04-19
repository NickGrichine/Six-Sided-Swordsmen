using UnityEngine;
using UnityEngine.UI;
using System;

public class SetupManager : Singleton<SetupManager>
{
    [SerializeField] private Button FinishSetupButton;

    public Player CurrentPlayer { get; private set; } = Player.PLAYER_1;

    private int _player_length = Enum.GetValues(typeof(Player)).Length;
    private int _player_index = 1;
    private int _save_slot = 0;



    void Start()
    {
        FinishSetupButton.onClick += (_) => EndSetupForCurrentPlayer();
        FinishSetupButton.onClick += (_) => ResourceManager.Instance.UpdateResourceDisplay();
    }

    public void EndSetupForCurrentPlayer()
    {
        _player_index++;
        if (_player_index >= _player_length) EndSetupPhase();
        CurrentPlayer = (Player)_player_index;
        Debug.Log("Current player is " + CurrentPlayer);
    }

    public void EndSetupPhase()
    {
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

        SaveData saveData = new SaveData("Setup Cache", -1, hex_grid_manager);
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


