using UnityEngine;

[DefaultExecutionOrder(100)]
public class ShowcaseSetup : MonoBehaviour
{
    [SerializeField] private bool spawnShowcaseOnStart = false;

    [SerializeField] private HexGridManager grid;
    [SerializeField] private UnitSpawner unitSpawner;
    
    private static readonly Vector2Int ArmyOneCenter = new Vector2Int(20, 13);
    private static readonly Vector2Int ArmyTwoCenter = new Vector2Int(20, 27);

    

    private static readonly Vector2Int[] FormationOffsets =
    {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(0, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1),
    };

    private static readonly UnitDataSO.UnitType[] ArmyComposition =
    {
        UnitDataSO.UnitType.Archer,
        UnitDataSO.UnitType.Archer,
        UnitDataSO.UnitType.Archer,
        UnitDataSO.UnitType.Swordsman,
        UnitDataSO.UnitType.Swordsman,
        UnitDataSO.UnitType.Spearman,
        UnitDataSO.UnitType.Spearman,
        UnitDataSO.UnitType.Knight,
        UnitDataSO.UnitType.Knight,
    };

    private void Start()
    {
        if (!spawnShowcaseOnStart)
            return;
        
        if (unitSpawner == null)
        {
            unitSpawner = GetComponent<UnitSpawner>() ?? gameObject.AddComponent<UnitSpawner>();
        }

        if (grid == null)
        {
            Debug.LogError("ShowcaseSetup: Grid is not assigned.");
            return;
        }

        unitSpawner.grid = grid;

        ClearExistingUnits();

        SpawnArmy(Player.PLAYER_1, ArmyOneCenter);
        SpawnArmy(Player.PLAYER_2, ArmyTwoCenter);

        GameManager.Instance?.NotifyGameStateChanged();

        Debug.Log($"ShowcaseSetup: spawned showcase armies near {ArmyOneCenter} and {ArmyTwoCenter}.");
    }

    public void SetSpawnShowcaseOnStart(bool value)
    {
        spawnShowcaseOnStart = value;
    }

    private void ClearExistingUnits()
    {
        UnitController[] existingUnits = FindObjectsOfType<UnitController>();
        foreach (UnitController unit in existingUnits)
        {
            if (unit != null)
            {
                if (unit.position != null && ReferenceEquals(unit.position.occupant, unit))
                {
                    unit.position.occupant = null;
                }

                Destroy(unit.gameObject);
            }
        }
    }

    private void SpawnArmy(Player team, Vector2Int center)
    {
        for (int i = 0; i < ArmyComposition.Length; i++)
        {
            Vector2Int spawnPos = center + FormationOffsets[i];
            UnitDataSO.UnitType unitType = ArmyComposition[i];

            UnitController spawned = unitSpawner.SpawnUnit(team, spawnPos, unitType);
            if (spawned == null)
            {
                Debug.LogWarning($"ShowcaseSetup: failed to spawn {unitType} for {team} near {spawnPos}.");
            }
        }
    }
}
