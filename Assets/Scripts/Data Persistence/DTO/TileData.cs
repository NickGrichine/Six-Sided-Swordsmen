using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
// Data-Transferable-Objects
public class TileData{
    // Stores one tile's persistent state
    public int tileId;
    public TileType type;
    public int altitude;
    public int grassVariant;
    public Vector2Int gridPos;
    public int moveCost;
    public bool passable;
    public List<int> neighborIds;
    public bool hasOccupant;
    public TileOccupantData occupant;
}