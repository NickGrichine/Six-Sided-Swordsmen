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
        Tile tile = FindPassableTileNear(axialPos);
        if (tile == null)
        {
            Debug.LogError($"No passable tile found near {axialPos}");
            return null;
        }

        GameObject go = Instantiate(unitPrefab);
        var unit = go.GetComponent<UnitController>();
        unit.teamID = team;
        if (tile.TryEnter(unit))
        {
            return unit;
        }

        Destroy(go);
        Debug.LogError($"TryEnter failed for tile at {tile.axialPos}");
        return null;
    }

    private Tile FindPassableTileNear(Vector2Int center)
    {
        // Check center first
        Tile tile = grid.GetTileAt(center);
        if (tile != null && !tile.IsNull && tile.passable && !tile.IsOccupied)
            return tile;

        // Check neighbors
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        foreach (var dir in directions)
        {
            Vector2Int pos = center + dir;
            tile = grid.GetTileAt(pos);
            if (tile != null && !tile.IsNull && tile.passable && !tile.IsOccupied)
                return tile;
        }

        return null;
    }
}