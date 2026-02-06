using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour  
{
    public TileType type;
    public int altitude = 0;

    public SpriteRenderer spriteRenderer;

    public List<Tile> neighbors = new List<Tile>(6); // 6 hex neighbors
    public Vector2Int axialPos; // q (col), r (row) for axial coords

    public bool passable = true; // Checks whether tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

    public IOccupant occupant;
    public bool IsOccupied => occupant != null;
    public bool BlockSight =>
        altitude >= 2 ||
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

    public bool CanClimbFrom(Tile from)
    {
        int heightDiff = Mathf.Abs(altitude - from.altitude);
        return heightDiff < 2;
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

        if (unit.CurrentTile != null && !CanClimbFrom(unit.CurrentTile))
        {
            Debug.Log($"Can't climb {Mathf.Abs(altitude - unit.CurrentTile.altitude)} height!");
            return false;
        }

        occupant = unit;
        unit.CurrentTile = this;
        return true;
    }
}
