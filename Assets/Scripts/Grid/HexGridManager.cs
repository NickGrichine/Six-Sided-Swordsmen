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
            case TileType.OCEAN:
                tile.altitude = 0;
                tile.passable = false;
                break;
            case TileType.GRASSLAND:
            case TileType.DESERT:
                tile.altitude = Random.Range(0, 1);
                tile.passable = true;
                break;
            case TileType.FOREST:
                tile.altitude = Random.Range(0, 2);
                break;
            case TileType.HILL:
                tile.altitude = Random.Range(1, 3);
                break;
            case TileType.MOUNTAIN:
                tile.altitude = Random.Range(2, 4);
                tile.passable = false;
                break;
        }


        // Set sprite, colour coded for now
        if (tile.spriteRenderer != null)
        {
            tile.spriteRenderer.color = tile.type switch
            {
                TileType.OCEAN => new Color(0.1f, 0.3f, 0.8f),     // Deep Ocean Blue
                TileType.GRASSLAND => new Color(0.3f, 0.8f, 0.3f), // Bright Grass Green
                TileType.DESERT => new Color(1f, 0.85f, 0.4f),     // Golden Desert Sand
                TileType.FOREST => new Color(0.2f, 0.6f, 0.2f),    // Dark Forest Green
                TileType.HILL => new Color(0.6f, 0.5f, 0.3f),      // Earthy Hill Brown
                TileType.MOUNTAIN => new Color(0.4f, 0.4f, 0.4f),  // Slate Gray Mountain
                _ => Color.white                                    // Fallback
            };
            
            Debug.Log($"Tile({q},{r}): Colored {tile.type} at {tile.transform.position}");
        }
        else
        {
            Debug.LogError($"Tile({q},{r}): No SpriteRenderer!");
        }
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