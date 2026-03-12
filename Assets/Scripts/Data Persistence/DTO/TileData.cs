using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]
public class TileData{
    public bool isNull;
    public TileType type;
    public int altitude;
    public SpriteRenderer spriteRenderer;
    public List<Tile> neighbors;
    public Vector2Int axialPos;
    public int moveCost = 1;
    public IOccupant occupant;
}