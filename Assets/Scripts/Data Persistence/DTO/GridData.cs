using System;
using UnityEngine;
using System.Collections.Generic;


[Serializable]
public class GridData{
    //DTO: Data-Transferable-Objects
    public int width;
    public int height;
    public float hexSize;
    public int oceanBorderThickness;
    public int cameraBorderTiles;

    // List of all TileData objects in tiles
    public List<TileData> tiles = new List<TileData>();

}