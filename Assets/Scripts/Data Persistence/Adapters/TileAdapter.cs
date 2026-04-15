using System;
using System.Collections.Generic;

public static class TileAdapter{
    public static TileData ToData(Tile tile){
        if (tile == null)
        {
            return null;
        }

        var data = new TileData{
            tileId = tile.tileId,
            type = tile.type,
            altitude = tile.altitude,
            gridPos = tile.gridPos,
            moveCost = tile.moveCost,
            passable = tile.passable,
            neighborIds = new List<int>(tile.neighborIds),
            occupant = ToOccupantData(tile.occupant)
        };

        return data;
    }

    public static void FromData(Tile tile, TileData tileData){
        //todo
        //tile.IsNull = tileData.isNull;
        tile.type = tileData.type;
        tile.altitude = tileData.altitude;
        tile.gridPos = tileData.gridPos;
        tile.moveCost = tileData.moveCost;
        //tile.occupant = tileData.occupant;
        tile.passable = tileData.passable;
        

    }

    private static TileOccupantData ToOccupantData(IOccupant occupant)
    {
        if (!(occupant is UnitController unit))
        {
            return null;
        }

        int currentHealth = unit.healthManager != null ? unit.healthManager.GetHealth() : 0;
        int maxHealth = unit.healthManager != null ? unit.healthManager.GetMaxHealth() : 0;
        int attackRange = unit.refData != null ? unit.refData.attackRange : unit.range;
        int attackStrength = unit.refData != null ? unit.refData.attackStr : 0;

        return new TileOccupantData
        {
            unitId = ReplayManager.GetOrCreatePersistentUnitId(unit),
            unitName = unit.name.Replace("(Clone)", string.Empty).Trim(),
            ownerId = unit.OwnerId,
            health = currentHealth,
            maxHealth = maxHealth,
            movesRemaining = unit.movesRemaining,
            attackRange = attackRange,
            attackStrength = attackStrength
        };
    }
}