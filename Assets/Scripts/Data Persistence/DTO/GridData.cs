using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]
public class GridData{
    //DTO: Data-Transferable-Objects
    //public List<TileData> tiles; //Grid container
    //public Tile tilePrefab;
    public int width;
    public int height;
    public float hexSize;
    public int oceanBorderThickness;
    public int cameraBorderTiles;

    // Sprites can be disregarded from data persistence as they are retrievable
    // public Sprite floorSprite;
    // public Sprite wallSprite;
}