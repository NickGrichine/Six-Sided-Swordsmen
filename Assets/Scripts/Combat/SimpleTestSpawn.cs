using UnityEngine;

public class SimpleTestSpawn : MonoBehaviour
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private bool spawnOnStart = true;

    [SerializeField] private Vector2Int player1Spawn = new Vector2Int(20, 13);
    [SerializeField] private Vector2Int player2Spawn = new Vector2Int(20, 27);

    [SerializeField] private UnitSpawner.TagUnitType player1Type = UnitSpawner.TagUnitType.Swordsman;
    [SerializeField] private UnitSpawner.TagUnitType player2Type = UnitSpawner.TagUnitType.Swordsman;

    private void Start()
    {
        if (!spawnOnStart)
            return;

        if (grid == null)
        {
            grid = HexGridManager.Instance;
        }

        if (unitSpawner == null)
        {
            unitSpawner = FindObjectOfType<UnitSpawner>();
        }

        if (grid == null || unitSpawner == null)
        {
            Debug.LogError("SimpleTestSpawn: grid or unitSpawner missing.");
            return;
        }

        unitSpawner.grid = grid;

        UnitController p1 = unitSpawner.SpawnUnit(Player.PLAYER_1, player1Spawn, player1Type);
        if (p1 == null)
        {
            Debug.LogWarning($"SimpleTestSpawn: failed to spawn PLAYER_1 at {player1Spawn}");
        }

        UnitController p2 = unitSpawner.SpawnUnit(Player.PLAYER_2, player2Spawn, player2Type);
        if (p2 == null)
        {
            Debug.LogWarning($"SimpleTestSpawn: failed to spawn PLAYER_2 at {player2Spawn}");
        }

        GameManager.Instance?.NotifyGameStateChanged();
    }
}