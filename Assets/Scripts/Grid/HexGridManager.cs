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
                grid[q, r] = CreateTile(q, r);
            }
        }

        // Link neighbors (6 directions for pointy-top hex)
        LinkNeighbors();
    }

    Tile CreateTile(int q, int r)
    {
        Tile tile = Instantiate(tilePrefab, transform);
        tile.axialPos = new Vector2Int(q, r);
        // tile.hexSize = hexSize; //position-only version

        // Position
        float xPos = hexSize * 1.5f * q;
        float yPos = hexSize * Mathf.Sqrt(3) * (r + 0.5f * (q % 2));
        tile.transform.localPosition = new Vector3(xPos, yPos, 0);

        // Randomize type [CUSTOMIZE HERE]
        tile.type = (TileType)Random.Range( 
            0, System.Enum.GetNames(typeof(TileType)).Length
        );
        
        switch (tile.type)
        {
            case TileType.Ocean:
                tile.altitude = AltitudeLevel.Low;
                tile.passable = false;
                break;
            case TileType.Grassland:
            case TileType.Desert:
                tile.altitude = AltitudeLevel.Low;
                tile.passable = true;
                break;
            case TileType.Forest:
            case TileType.Hill:
                tile.altitude = AltitudeLevel.Medium;
                break;
            case TileType.Mountain:
                tile.altitude = ALtitudeLevel.Impassable;
                tile.passable = false;
                tile.BlockSight = true;
                break;
        }


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
        foreach (Transform child in transform) 
            Destroy(child.gameObject);
    }

    void Start()
    {
        GenerateGrid();
    }

    public IEnumerable<Tile> GetAllTiles()
    {
        for (int q = 0; q < width; q++)
            for (int r = 0; r < height; r++)
                if (grid[q, r] != null)
                    yield return grid[q, r];
    }
}