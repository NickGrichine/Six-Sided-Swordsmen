using UnityEngine;
using System.Collections.Generic;
using Script.Units;

public class Tile : MonoBehaviour  
{
    public TileType type;
    public AltitudeLevel altitude = AltitudeLevel.Low;

    public SpriteRenderer spriteRenderer;

    public List<Tile> neighbors = new List<Tile>(6); // 6 hex neighbors
    public Vector2Int axialPos; // q (col), r (row) for axial coords

    public bool passable = true; // Checks whether tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

    public IOccupant occupant;
    public bool IsOccupied => occupant != null;
    public bool BlockSight =>
        altitude >= AltitudeLevel.High ||
        type == TileType.Mountain; // Mountain always blocks


    // Start (Unity lifecycle method)
    private void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // Static method for distnce 
    public static int GetDistance(Tile a, Tile b)
    {
        int dq = a.axialPos.x - b.axialPos.x;
        int dr = a.axialPos.y - b.axialPos.y;
        int ds = -dq - dr;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    // Add neighbor if not included in neighbors
    public void AddNeighbor(Tile neighbor)
    {
        if (!neighbors.Contains(neighbor)) neighbors.Add(neighbor);
    }

    // Checks about occupied
    public bool TryEnter(IOccupant unit)
    {
        if (!passable || IsOccupied) return false;
        occupant = unit;
        unit.CurrentTile = this;
        return true;
    }
}
