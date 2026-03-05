using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public HexGridManager grid;
    public GameObject unitPrefab;

    void Start()
    {
        // we spawn two units on different teams. DO NOTE THIS IS NOT THE COMMANDPROCESSOR.
        SpawnUnit(Team.Player1, new Vector2Int(2, 3));
        SpawnUnit(Team.Player2, new Vector2Int(5, 7));
    }

    public UnitController SpawnUnit(Team team, Vector2Int axialPos)
    {
        Tile tile = grid.GetTileAt(axialPos);
        if (tile == null || tile.IsOccupied || !tile.passable) return null;

        GameObject go = Instantiate(unitPrefab);
        var unit = go.GetComponent<UnitController>();
        unit.teamID = team;
        if (tile.TryEnter(unit))
        {
            return unit;
        }

        Destroy(go);
        return null;
    }
}