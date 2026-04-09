using System;
using System.Collections.Generic;

public static class GridAdapter{
    
    public static GridData ToData(HexGridManager grid){

        // Builds a GridData object from the HexGridManager grid
        var data = new GridData {
            width = grid.width,
            height = grid.height,
            hexSize = grid.hexSize,
            tiles = new List<TileData>(),
            oceanBorderThickness = grid.OceanBorderThickness,
            cameraBorderTiles = grid.CameraBorderTiles,
        };

        // Populate the tile list (checks non-null tile)
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
        
        
        if (grid == null || gridData == null) 
            return grid;

        // Restores all grid settings
        grid.RestoreGridSettings(
            gridData.width,
            gridData.height,
            gridData.hexSize,
            gridData.oceanBorderThickness,
            gridData.cameraBorderTiles
        );

        // Recreate the runtime tile objects for a grid of the correct size
        grid.GenerateGrid();

        // Populate the grid with saved tile data
        if (gridData.tiles != null)
        {
            foreach (TileData tileData in gridData.tiles)
            {
                Tile tile = grid.GetTileById(tileData.tileId);
                if (tile != null)
                {
                    // Overwrite that tile with the saved state
                    TileAdapter.FromData(tile, tileData);
                }
            }
        }

        // Reconstruct tile neighbors from 
        grid.RebuildNeighborsFromIds();

        // Refresh tile visuals and camera settings
        grid.RefreshAfterLoad();

        return grid;
    }
}