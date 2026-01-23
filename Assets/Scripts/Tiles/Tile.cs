using UnityEngine;
using System.Collection.Generic;

public class Tile: MonoBehaviour
{
    
    public TileType type;
    public SpriteRenderer spriteRenderer;
    public list<Tile> neighbors = new list<Tile>(6); // 6 hex neighbors
    public Vector2Int axialPos; // q (col), r (row) for axial coords

    public boolean passable = true; // Checks whether if the tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

    private void start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<spriteRenderer>();
        }
    }

    public static int GetDistance(Tile a, Tile b) // 
    {
        int dq = a.axialPos.x - b.axialPos.x;
        int dr = a.axialPos.y - b.axialPos.y;
        int ds = -dq - dr;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    public void AddNeighbor(Tile neighbor)
    {
        if (!neighbors.Contains(neighbor)) neighbors.Add(neighbor);
    }
}