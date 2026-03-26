using System;
using System.Collections.Generic;

public static class TileAdapter{
    public static TileData ToData(Tile tile){
        //todo
        var data = new TileData{
            //isNull = tile.IsNull,
            type = tile.type,
            altitude = tile.altitude,
            axialPos = tile.axialPos,
            moveCost = tile.moveCost,
            occupant = tile.occupant

        };
        return data;
        
    }
    public static void FromData(Tile tile, TileData tileData){
        //todo
        //tile.IsNull = tileData.isNull;
        tile.type = tileData.type;
        tile.altitude = tileData.altitude;
        tile.axialPos = tileData.axialPos;
        tile.moveCost = tileData.moveCost;
        tile.occupant = tileData.occupant;
        //tile.IsOccupied = tileData.IsOccupied;
        //tile.BlockSight = tile.BlockSight;

    }
}