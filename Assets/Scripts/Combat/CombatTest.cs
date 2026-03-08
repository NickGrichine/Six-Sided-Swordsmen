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
        // Destroy any leftover units from previous runs. DO NOT delete this forloop unless you do something of a similar function.
        foreach (var unit in FindObjectsOfType<UnitController>())
        {
            Destroy(unit.gameObject);
        }

        spawner = gameObject.AddComponent<UnitSpawner>();
        spawner.grid = grid;
        spawner.unitPrefab = unitPrefab;

        unitA = spawner.SpawnUnit(Team.Player1, new Vector2Int(0, 0));
        unitB = spawner.SpawnUnit(Team.Player2, new Vector2Int(2, 2));

        Debug.Log($"A at {unitA?.position.axialPos}, B at {unitB?.position.axialPos}");
    }

    void Update()
    {
        // press Space to perform an attack and print the result
        if (Input.GetKeyDown(KeyCode.Space) && unitA != null && unitB != null)
        {
            bool success = unitA.Attack(unitB);
            Debug.Log($"Attack called: success={success}, B health={unitB.healthManager.GetHealth()}");
        }
    }
}