using UnityEngine;
using System.Collections.Generic;  // Fixed: Collection → Collections

public class Tile : MonoBehaviour  // Fixed spacing
{
    public TileType type;
    public SpriteRenderer spriteRenderer;

    // Fixed: list → List (capital L)
    public List<Tile> neighbors = new List<Tile>(6); // 6 hex neighbors
    public Vector2Int axialPos; // q (col), r (row) for axial coords

    // Fixed: boolean → bool
    public bool passable = true; // Checks whether tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

    // Fixed: start → Start (Unity lifecycle method)
    private void Start()
    {
        if (spriteRenderer == null)
        {
            // Fixed: spriteRenderer → SpriteRenderer (capital S)
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public static int GetDistance(Tile a, Tile b)
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
