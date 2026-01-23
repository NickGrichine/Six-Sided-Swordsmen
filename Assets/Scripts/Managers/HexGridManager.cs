using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public Tile tilePrefab;
    public int width = 10;  // q cols
    public int height = 10;  // r rows
    public float hexSize = 1f;

    [Header("Sprites by Type")]
    public Sprite floorSprite;
    public Sprite wallSprite;

    private Tile[,] grid;

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();
        grid = new Tile[width, height];

        // Generate tiles with axial positions
        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                grid[q,r] = CreateTile(q, r);
            }
        }

        // Link neighbors (6 directions for pointy-top hex)
        LinkNeighbors();
    }

    Tile CreateTile(int q, int r)
    {
        Tile tile = Instantiate(tilePrefab, transform);
        tile.axialPos = new Vector2Int(q, r);
        tile.hexSize = hexSize; //position-only version

        // Position
        float xPos = hexSize * 1.5f * q;
        float yPos = hexSize * Mathf.Sqrt(3) * (r + 0.5f * (q % 2));
        tile.transform.localPosition = new Vector3(xPos, yPos, 0);

        // Randomize type [CUSTOMIZE HERE]
        tile.type = (Tiletype)Random.range(0, System.Enum.GetNames(typeof(TileType)).Length);
        if (tile.type == TileType.Wall) tile.passable = false;

        // Set sprite
        tile.spriteRenderer.sprite = tile.type = TileType.Wall ? wallSprite : floorSprite;

        return tile;
    }

    void LinkNeighbors()
    {
        Vector2Int[] directions =
        {
            new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                Tile tile = grid[q, r];
                foreach (Vector2Int dir in directions)
                {
                    int nq = q + dir.x;
                    int nr = r + dir.y;
                    if (nq >= 0 && nq < width && nr >= 0 && nr < height)
                    {
                        tile.AddNeighbor(grid[nq, nr]);
                    }
                }
            }
        }
    }

    void ClearGrid()
    {
        foreach (Transform child in transform) DestroyImmediate(child.gameObject);
    }

    void Start()
    {
        GenerateGrid();
    }
}