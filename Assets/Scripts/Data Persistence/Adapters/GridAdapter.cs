using System;
using System.Collections.Generic;

public static class GridAdapter{
    
    public static GridData ToData(HexGridManager grid){
        if (grid == null || grid.Grid == null)
        {
            return null;
        }

        var data = new GridData {
            width = grid.width,
            height = grid.height,
            hexSize = grid.hexSize,
            oceanBorderThickness = grid.OceanBorderThickness,
            cameraBorderTiles = grid.CameraBorderTiles,
        };

        foreach (var tile in grid.Grid)
        {
            TileData tileData = TileAdapter.ToData(tile);
            if (tileData != null)
            {
                data.tiles.Add(tileData);
            }
        }
        return data;
    }

    public static HexGridManager FromData(HexGridManager grid, GridData gridData){
        //todo
        grid.width = gridData.width;
        grid.height = gridData.height;
        grid.hexSize = gridData.hexSize;
        //todo Tiles
        //grid.OceanBorderThickness = gridData.oceanBorderThickness,
        //grid.CameraBorderTiles = gridData.cameraBorderTiles,
        

        return grid;
    }
}