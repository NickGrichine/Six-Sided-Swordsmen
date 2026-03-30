using System;
using System.Collections.Generic;

public static class TileAdapter{
    public static TileData ToData(Tile tile){
        //todo
        var data = new TileData{
            //isNull = tile.IsNull,
            type = tile.type,
            altitude = tile.altitude,
            gridPos = tile.gridPos,
            moveCost = tile.moveCost,
            passable = tile.passable,

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