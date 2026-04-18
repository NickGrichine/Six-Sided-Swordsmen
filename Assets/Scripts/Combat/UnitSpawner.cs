using TMPro;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public HexGridManager grid;
    public GameObject unitPrefab;
    public GameObject knightPrefab;
    public GameObject archerPrefab;
    public GameObject swordsmanPrefab;
    public GameObject spearmanPrefab;

    public enum TagUnitType
    {
        Knight, Archer, Swordsman, Spearman
    }

    public UnitController SpawnUnit(Player team, Vector2Int gridPos)
    {
        Tile tile = FindPassableTileNear(gridPos);
        if (tile == null)
        {
            Debug.LogError($"No passable tile found near {gridPos}");
            return null;
        }

        return SpawnUnitOnTile(team, tile, unitPrefab, recordReplay: true, notifyGameState: true);
    }

    public UnitController SpawnUnit(Player team, Vector2Int gridPos, TagUnitType unitType)
    {
        GameObject prefab = GetPrefabForType(unitType);
        if (prefab == null)
        {
            Debug.LogError($"SpawnUnit failed: no prefab found for unit type {unitType}.");
            return null;
        }

        Tile tile = FindPassableTileNear(gridPos);
        if (tile == null)
        {
            Debug.LogError($"No passable tile found near {gridPos}");
            return null;
        }

        return SpawnUnitOnTile(team, tile, prefab, recordReplay: true, notifyGameState: true);
    }

    private Tile FindPassableTileNear(Vector2Int center)
    {
        // Check center first
        Tile tile = grid.GetTileAt(center);
        if (tile != null && tile.passable && !tile.IsOccupied)
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
            if (tile != null && tile.passable && !tile.IsOccupied)
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
            case TagUnitType.Swordsman:
                return swordsmanPrefab != null ? swordsmanPrefab : unitPrefab;
            case TagUnitType.Spearman:
                return spearmanPrefab != null ? spearmanPrefab : unitPrefab;
            default:
                return unitPrefab;
        }
    }

    private GameObject GetPrefabForType(UnitDataSO.UnitType unitType)
    {
        switch (unitType)
        {
            case UnitDataSO.UnitType.Knight:
                return knightPrefab != null ? knightPrefab : unitPrefab;
            case UnitDataSO.UnitType.Archer:
                return archerPrefab != null ? archerPrefab : unitPrefab;
            case UnitDataSO.UnitType.Swordsman:
                return swordsmanPrefab != null ? swordsmanPrefab : unitPrefab;
            case UnitDataSO.UnitType.Spearman:
                return spearmanPrefab != null ? spearmanPrefab : unitPrefab;
            default:
                return unitPrefab;
        }
    }

    public UnitController SpawnUnitFromSave(Tile tile, TileOccupantData occupantData)
    {
        if (tile == null || occupantData == null)
            return null;

        if (tile.IsOccupied)
        {
            Debug.LogWarning($"SpawnUnitFromSave failed: tile {tile.gridPos} is already occupied.");
            return null;
        }

        GameObject prefab = GetPrefabForType(occupantData.unitType);
        if (prefab == null)
        {
            Debug.LogError($"SpawnUnitFromSave failed: no prefab found for unit type {occupantData.unitType}.");
            return null;
        }

        UnitController unit = SpawnUnitOnTile(
            (Player)occupantData.ownerId,
            tile,
            prefab,
            recordReplay: false,
            notifyGameState: false
        );

        if (unit == null)
            return null;

        if (unit.healthManager != null)
        {
            unit.healthManager.SetMaxHealth(occupantData.maxHealth);
            unit.healthManager.SetCurrentHealth(occupantData.health);
        }

        unit.movesRemaining = occupantData.movesRemaining;
        unit.range = occupantData.attackRange;
        unit.name = string.IsNullOrWhiteSpace(occupantData.unitName) ? prefab.name : occupantData.unitName;

        return unit;
    }

    private UnitController SpawnUnitOnTile(Player team, Tile tile, GameObject prefab, bool recordReplay = true, bool notifyGameState = true)
    {
        if (tile == null)
        {
            Debug.LogError("SpawnUnitOnTile failed: tile is null.");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError("SpawnUnitOnTile failed: prefab is null.");
            return null;
        }

        if (tile.IsOccupied)
        {
            Debug.LogWarning($"SpawnUnitOnTile failed: tile {tile.gridPos} is already occupied.");
            return null;
        }

        if (!tile.passable)
        {
            Debug.LogWarning($"SpawnUnitOnTile failed: tile {tile.gridPos} is not passable.");
            return null;
        }

        GameObject go = Instantiate(prefab);
        UnitController unit = go.GetComponent<UnitController>();

        if (unit == null)
        {
            Debug.LogError($"SpawnUnitOnTile failed: prefab '{prefab.name}' has no UnitController.");
            Destroy(go);
            return null;
        }

        unit.SetTeam(team);

        if (!tile.TryEnter(unit))
        {
            Debug.LogWarning($"SpawnUnitOnTile failed: TryEnter failed for tile {tile.gridPos}.");
            Destroy(go);
            return null;
        }

        if (recordReplay)
        {
            ReplayManager.EnsureExists().RecordUnitSpawned(unit, tile);
        }

        if (notifyGameState)
        {
            GameManager.Instance?.NotifyGameStateChanged();
        }

        return unit;
    }
}
