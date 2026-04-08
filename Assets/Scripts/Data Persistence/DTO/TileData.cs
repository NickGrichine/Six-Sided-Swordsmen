using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]
public class TileData{
    //Data-Transferable-Objects
    public int tileId;
    public TileType type;
    public int altitude;
    //public SpriteRenderer spriteRenderer;
    public int grassVariant;
    public Vector2Int gridPos;
    public int moveCost;
    public bool passable;
    public List<int> neighborIds = new List<int>();

}