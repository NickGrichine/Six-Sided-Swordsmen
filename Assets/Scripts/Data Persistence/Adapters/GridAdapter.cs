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
        //TODO: NEED GRID TO BE PUBLIC FIELD
        // foreach (var tile in grid.grid)
        // {
        //     TileAdapter.ToData(tile);
        // }
        return data;
    }

    public static HexGridManager FromData(HexGridManager grid, GridData gridData){
        //todo
        grid.width = gridData.width;
        grid.height = gridData.height;
        grid.hexSize = gridData.hexSize;

        return grid;
    }
}