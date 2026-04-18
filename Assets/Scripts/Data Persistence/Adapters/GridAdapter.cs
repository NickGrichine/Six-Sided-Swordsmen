using System;
using System.Collections.Generic;
using UnityEngine;

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

        grid.MarkLoadedFromSave();

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

        grid.ClearAllOccupants();

        RebuildUnits(grid, gridData);

        // Refresh tile visuals and camera settings
        grid.RefreshAfterLoad();

        return grid;
    }

    private static void RebuildUnits(HexGridManager grid, GridData gridData)
    {
        UnitController[] existingUnits = UnityEngine.Object.FindObjectsOfType<UnitController>();
        foreach (UnitController unit in existingUnits)
        {
            if (unit != null)
            {
                if (unit.position != null && ReferenceEquals(unit.position.occupant, unit))
                {
                    unit.position.occupant = null;
                }

                UnityEngine.Object.Destroy(unit.gameObject);
            }
        }

        if (gridData.tiles == null)
            return;

        UnitSpawner spawner = UnityEngine.Object.FindObjectOfType<UnitSpawner>();
        if (spawner == null)
        {
            Debug.LogError("GridAdapter: No UnitSpawner found in scene, cannot restore units from save.");
            return;
        }

        spawner.grid = grid;

        foreach (TileData tileData in gridData.tiles)
        {
            if (tileData == null || tileData.occupant == null)
                continue;

            Tile tile = grid.GetTileById(tileData.tileId);
            if (tile == null)
                continue;

            if (tile.IsOccupied)
                continue;

            spawner.SpawnUnitFromSave(tile, tileData.occupant);
        }

    }


}