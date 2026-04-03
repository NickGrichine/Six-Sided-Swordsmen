using System;
using System.Collections.Generic;

public static class TileAdapter{
    public static TileData ToData(Tile tile){
        //todo
        var data = new TileData{
            //isNull = tile.IsNull,
            tileId = tile.tileId,
            type = tile.type,
            altitude = tile.altitude,
            gridPos = tile.gridPos,
            moveCost = tile.moveCost,
            passable = tile.passable,
            neighborIds = new List<int>(tile.neighborIds)
        };
        //foreach(var tileId in tile.tiles)
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
}