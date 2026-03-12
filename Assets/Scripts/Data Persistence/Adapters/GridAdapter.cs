using System;
using System.Collections.Generic;

public static class GridAdapter{
    
    public static GridData ToData(HexGridManager grid){
        //todo
        var data = new GridData {
            width = grid.width,
            height = grid.height,
            hexSize = grid.hexSize,
            tiles = new List<TileData>()
        };
        // foreach (var tile in grid.grid)
        // {
        //     TileAdapter.ToData(tile);
        // }
        return data;
    }

    public static void FromData(HexGridManager grid){
        //todo
    }
}