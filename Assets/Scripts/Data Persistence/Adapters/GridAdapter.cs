using System;
using System.Collections.Generic;

public static class GridAdapter{
    
    public static GridData ToData(HexGridManager grid){
        //todo
        var data = new GridData {
            width = grid.width,
            height = grid.height,
            hexSize = grid.hexSize,
            tiles = new List<TileData>(),
            oceanBorderThickness = grid.OceanBorderThickness,
            cameraBorderTiles = grid.CameraBorderTiles,
        };

        //TODO: NEED GRID TO BE PUBLIC FIELD
        foreach (var tile in grid.Grid)
        {
            if (tile != null)
            {
                data.tiles.Add(TileAdapter.ToData(tile));

            }
        }
        return data;
    }

    public static HexGridManager FromData(HexGridManager grid, GridData gridData){
        //todo
        // grid.width = gridData.width;
        // grid.height = gridData.height;
        // grid.hexSize = gridData.hexSize;
        //todo Tiles
        //grid.OceanBorderThickness = gridData.oceanBorderThickness,
        //grid.CameraBorderTiles = gridData.cameraBorderTiles,
        
        if (grid == null || gridData == null) 
            return grid;

        grid.RestoreGridSettings(
            gridData.width,
            gridData.height,
            gridData.hexSize,
            gridData.oceanBorderThickness,
            gridData.cameraBorderTiles
        );

        grid.GenerateGrid();

        if (gridData.tiles != null)
        {
            foreach (TileData tileData in gridData.tiles)
            {
                Tile tile = grid.GetTileById(tileData.tileId);
                if (tile != null)
                {
                    TileAdapter.FromData(tile, tileData);
                }
            }
        }

        grid.RebuildNeighborsFromIds();
        grid.RefreshAfterLoad();

        return grid;
    }
}