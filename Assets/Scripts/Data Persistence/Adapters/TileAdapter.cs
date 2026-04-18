using System;
using System.Collections.Generic;
using UnityEngine;

public static class TileAdapter{
    public static TileData ToData(Tile tile)
    {
        if (tile == null)
        {
            return null;
        }

        TileOccupantData occupantData = ToOccupantData(tile.occupant);

        var data = new TileData
        {
            tileId = tile.tileId,
            type = tile.type,
            altitude = tile.altitude,
            grassVariant = tile.grassVariant,
            gridPos = tile.gridPos,
            moveCost = tile.moveCost,
            passable = tile.passable,
            neighborIds = new List<int>(tile.neighborIds),
            hasOccupant = occupantData != null,
            occupant = occupantData
        };

        return data;
    }

    public static void FromData(Tile tile, TileData tileData){
        if (tile == null || tileData == null)
            return;

        // Overwrites tile fields with the values from the save file
        // Save Tile -> Runtime Tile
        tile.tileId = tileData.tileId;
        tile.type = tileData.type;
        tile.altitude = tileData.altitude;
        tile.grassVariant = tileData.grassVariant;
        tile.gridPos = tileData.gridPos;
        tile.moveCost = tileData.moveCost;
        tile.passable = tileData.passable;

        tile.neighborIds = tileData.neighborIds != null
            ? new List<int>(tileData.neighborIds)
            : new List<int>();

        tile.occupant = null;

    }

    private static TileOccupantData ToOccupantData(IOccupant occupant)
    {
        if (occupant == null)
            return null;

        UnitController unit = occupant as UnitController;
        if (unit == null)
            return null;

        UnityEngine.Object unityObj = unit as UnityEngine.Object;
        if (unityObj == null)
            return null;

        if (unit.CurrentTile == null)
            return null;

        if (!ReferenceEquals(unit.CurrentTile.occupant, unit))
            return null;

        int currentHealth = unit.healthManager != null ? unit.healthManager.GetHealth() : 0;
        int maxHealth = unit.healthManager != null ? unit.healthManager.GetMaxHealth() : 0;
        int attackRange = unit.refData != null ? unit.refData.attackRange : unit.range;
        int attackStrength = unit.refData != null ? unit.refData.attackStr : 0;
        UnitDataSO.UnitType unitType = unit.refData != null ? unit.refData.unitType : UnitDataSO.UnitType.Swordsman;

        Debug.Log($"Saving unit {unit.name}: current={unit.healthManager?.GetHealth()} max={unit.healthManager?.GetMaxHealth()} tile={unit.CurrentTile?.gridPos}");
        
        return new TileOccupantData
        {
            unitId = ReplayManager.GetOrCreatePersistentUnitId(unit),
            unitName = string.IsNullOrWhiteSpace(unit.name) ? "Unit" : unit.name.Replace("(Clone)", string.Empty).Trim(),
            unitType = unitType,
            ownerId = unit.OwnerId,
            health = currentHealth,
            maxHealth = maxHealth,
            movesRemaining = unit.movesRemaining,
            attackRange = attackRange,
            attackStrength = attackStrength,
            refDataName = unit.refData != null ? unit.refData.name : string.Empty
        };
    }
}