using System;
using UnityEngine;
[Serializable]
public class GridData{
    public Tile[,] tiles; //Grid container
    public Tile tilePrefab;
    public int width;
    public int height;
    public float hexSize;
    public Sprite floorSprite;
    public Sprite wallSprite;
}