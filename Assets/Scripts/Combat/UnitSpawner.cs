using TMPro;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public HexGridManager grid;
    public GameObject unitPrefab;
    public GameObject knightPrefab;
    public GameObject archerPrefab;
    public GameObject clericPrefab;
    public GameObject spearmanPrefab;

    public enum TagUnitType
    {
        Knight, Archer, Cleric, Spearman
    }

    public UnitController SpawnUnit(Player team, Vector2Int axialPos, TagUnitType unitType)
    {
        GameObject previousPrefab = unitPrefab;
        unitPrefab = GetPrefabForType(unitType);
        UnitController spawned = SpawnUnit(team, axialPos);
        unitPrefab = previousPrefab;
        return spawned;
    }


    public UnitController SpawnUnit(Player team, Vector2Int axialPos)
    {
        Tile tile = FindPassableTileNear(axialPos);
        if (tile == null)
        {
            Debug.LogError($"No passable tile found near {axialPos}");
            return null;
        }

        GameObject go = Instantiate(unitPrefab);
        var unit = go.GetComponent<UnitController>();
        unit.SetTeam(team);
        
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

    private GameObject GetPrefabForType(TagUnitType unitType)
    {
        switch (unitType)
        {
            case TagUnitType.Knight:
                return knightPrefab != null ? knightPrefab : unitPrefab;
            case TagUnitType.Archer:
                return archerPrefab != null ? archerPrefab : unitPrefab;
            case TagUnitType.Cleric:
                return clericPrefab != null ? clericPrefab : unitPrefab;
            case TagUnitType.Spearman:
                return spearmanPrefab != null ? spearmanPrefab : unitPrefab;
            default:
                return unitPrefab;
        }
    }
}
