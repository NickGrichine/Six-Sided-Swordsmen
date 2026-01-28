using UnityEngine;
using System.Collections.Generic; 

public class Tile : MonoBehaviour  
{
    public TileType type;
    public SpriteRenderer spriteRenderer;
    
    public List<Tile> neighbors = new List<Tile>(6); // 6 hex neighbors
    public Vector2Int axialPos; // q (col), r (row) for axial coords

    public bool passable = true; // Checks whether tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

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
}
