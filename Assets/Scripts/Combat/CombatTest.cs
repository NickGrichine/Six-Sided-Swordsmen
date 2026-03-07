using UnityEngine;

public class CombatTest : MonoBehaviour
{
    public HexGridManager grid;      
    public GameObject unitPrefab;     

    private UnitController unitA;
    private UnitController unitB;
    private UnitSpawner spawner;

    void Start()
    {
        Debug.Log("CombatTest.Start called");
        var allUnits = FindObjectsOfType<UnitController>();
        Debug.Log($"Total UnitController in scene before destroy: {allUnits.Length}");
        foreach (var u in allUnits) Debug.Log($"Unit: {u.gameObject.name} at {u.transform.position}");

        if (FindObjectsOfType<CombatTest>().Length > 1)
        {
            Debug.LogError("Multiple CombatTest in scene, destroying this one");
            Destroy(gameObject);
            return;
        }

        // Destroy any leftover units from previous runs
        foreach (var unit in allUnits)
        {
            Destroy(unit.gameObject);
        }

        Debug.Log("After destroy, units left: " + FindObjectsOfType<UnitController>().Length);

        spawner = GetComponent<UnitSpawner>() ?? gameObject.AddComponent<UnitSpawner>();
        spawner.grid = grid;
        spawner.unitPrefab = unitPrefab;

        unitA = spawner.SpawnUnit(Team.Player1, new Vector2Int(0, 0));
        unitB = spawner.SpawnUnit(Team.Player2, new Vector2Int(2, 2));

        Debug.Log($"Spawned A at {unitA?.position.axialPos}, B at {unitB?.position.axialPos}");
        Debug.Log("Total units after spawn: " + FindObjectsOfType<UnitController>().Length);
    }

    void Update()
    {
        // press Space to perform an attack and print the result
        if (Input.GetKeyDown(KeyCode.Space) && unitA != null && unitB != null)
        {
            bool success = unitA.Attack(unitB);
            Debug.Log($"Attack called: success={success}, B health={unitB.healthManager.GetHealth()}");
        }

        // press M to move unitA to its first neighbor
        if (Input.GetKeyDown(KeyCode.M) && unitA != null && unitA.position.neighbors.Count > 0)
        {
            Tile dest = unitA.position.neighbors[0];
            bool moved = unitA.MoveToAdjacentTile(dest);
            Debug.Log($"Move A to {dest.axialPos}: {moved}, now at {unitA.position.axialPos}");
        }
    }
}