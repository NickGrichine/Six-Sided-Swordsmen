using TMPro;
using UnityEngine;

public class UnitSpawner : Singleton<UnitSpawner>
{
    public HexGridManager grid;
    public GameObject knightPrefab;
    public GameObject archerPrefab;
    public GameObject swordsmanPrefab;
    public GameObject spearmanPrefab;

    void Awake()
    {
        base.Awake();
        grid = HexGridManager.Instance;
    }

    public UnitController SpawnUnit(Player team, Tile tile, UnitDataSO.UnitType unitType)
    {
        GameObject prefab = GetPrefabForType(unitType);
        if (prefab == null)
        {
            Debug.LogError($"SpawnUnit failed: no prefab found for unit type {unitType}.");
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

    private GameObject GetPrefabForType(UnitDataSO.UnitType unitType)
    {
        switch (unitType)
        {
            case UnitDataSO.UnitType.Knight:
                return knightPrefab;
            case UnitDataSO.UnitType.Archer:
                return archerPrefab;
            case UnitDataSO.UnitType.Swordsman:
                return swordsmanPrefab;
            case UnitDataSO.UnitType.Spearman:
                return spearmanPrefab;
            default:
                return null;
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

        unit.ApplyLoadedState(
            occupantData.health,
            occupantData.maxHealth,
            occupantData.movesRemaining,
            occupantData.attackRange,
            string.IsNullOrWhiteSpace(occupantData.unitName) ? prefab.name : occupantData.unitName
        );

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
