using UnityEngine;

public class CombatTest : MonoBehaviour
{
    public HexGridManager grid;      
    public GameObject unitPrefab;     

    private UnitController unitA;
    private UnitController unitB;
    private UnitController unitC;
    private UnitController unitD;

    private UnitController unitE;

    private UnitController unitF;

    private UnitController unitG;
    private UnitController unitH;
    private UnitController unitI;


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
        unitB = spawner.SpawnUnit(Team.Player1, new Vector2Int(2, 1));
        unitC = spawner.SpawnUnit(Team.Player1, new Vector2Int(0, 2));
        unitD = spawner.SpawnUnit(Team.Player1, new Vector2Int(1, 3));
        unitE = spawner.SpawnUnit(Team.Player1, new Vector2Int(0, 4));
        unitF = spawner.SpawnUnit(Team.Player2, new Vector2Int(4, 6));
        unitG = spawner.SpawnUnit(Team.Player2, new Vector2Int(6, 5));
        unitH = spawner.SpawnUnit(Team.Player2, new Vector2Int(6, 8));
        unitI = spawner.SpawnUnit(Team.Player2, new Vector2Int(4, 7));


        Debug.Log($"Spawned A at {unitA?.position.axialPos}, B at {unitB?.position.axialPos}");
        Debug.Log("Total units after spawn: " + FindObjectsOfType<UnitController>().Length);
    }

    void Update()
    {
        // press Space to perform an attack and print the result
        if (Input.GetKeyDown(KeyCode.Space) && unitA != null && unitF != null)
        {
            bool success = unitA.Attack(unitF);
            Debug.Log($"Attack called: success={success}, B health={unitF.healthManager.GetHealth()}");
        }

        // press M to move unitA along a path to a target tile
        if (Input.GetKeyDown(KeyCode.M) && unitA != null)
        {
            // Find a target tile, e.g., 2 steps away
            var target = grid.GetTileAt(new Vector2Int(4, 5));
            if (target != null && target != unitA.position)
            {
                bool moved = unitA.MoveToTile(target);
                Debug.Log($"Move A to {target.axialPos}: {moved}, now at {unitA.position.axialPos}");
            }
        }
    }
}