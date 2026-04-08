using System;
using System.Collections.Generic;

public static class TileAdapter{
    public static TileData ToData(Tile tile){
        //todo
        return new TileData
        {
            tileId = tile.tileId,
            type = tile.type,
            altitude = tile.altitude,
            grassVariant = tile.grassVariant,
            gridPos = tile.gridPos,
            moveCost = tile.moveCost,
            passable = tile.passable,
            neighborIds = new List<int>(tile.neighborIds)
        };
        
    }
    public static void FromData(Tile tile, TileData tileData){
        //todo

        if (tile == null || tileData == null)
            return;

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
        

    }
}