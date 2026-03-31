using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]
public class TileData{
    //Data-Transferable-Objects
    public TileType type;
    public int altitude;
    //public SpriteRenderer spriteRenderer;
    public List<TileData> neighbors;
    public Vector2Int gridPos;
    public int moveCost;
    public bool passable;
    public List<int> neighborTileid;
    
}