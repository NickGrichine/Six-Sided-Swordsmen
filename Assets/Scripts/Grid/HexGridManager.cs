using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public Tile tilePrefab;
    public int width = 10;   // q cols
    public int height = 10;  // r rows
    public float hexSize = 1f;

    [Header("Sprites by Type")]
    public Sprite floorSprite;
    public Sprite wallSprite;

    public enum GenerationMode { Procedural, Static }

    [Header("Map Generation")]
    public GenerationMode generationMode = GenerationMode.Procedural;

    [Header("Biome Sprites")]
    public Sprite grassFlatSprite;      // Grass, no rocks
    public Sprite grassRock1Sprite;     // Grass, one rock
    public Sprite grassRock2Sprite;     // Grass, more rocks
    public Sprite grassFlowerSprite;    // Grass, flower

    public Sprite purpleFlowerSprite;   // Purple, flower
    public Sprite purpleMushroomSprite; // Purple, mushroom

    public Sprite shoreSprite;          // Shore
    public Sprite deepOceanSprite;      // Deep Ocean

    public Sprite mountainSprite;       // Mountain

    [Header("Selection Outline")]
    public Sprite selectionOutlineSprite;

    [Header("Generation Tuning")]
    [Range(0f, 1f)] public float oceanChance = 0.18f;
    [Range(0f, 1f)] public float purpleChance = 0.32f;
    [Range(0f, 0.5f)] public float grassVariantChance = 0.08f;
    [Range(0f, 0.5f)] public float purpleVariantChance = 0.08f;

    private Tile[,] grid;

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();
        grid = new Tile[width, height];

        TileType[,] plannedTypes = new TileType[width, height];

        GenerateBiomePlan(plannedTypes);

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                grid[q, r] = CreateTile(q, r, plannedTypes[q, r]);
            }
        }

        LinkNeighbors();
        ApplyShorelines();
        RefreshAllTileVisuals();
    }

    private void GenerateBiomePlan(TileType[,] plannedTypes)
    {
        float purpleNoiseOffsetX = Random.Range(0f, 999f);
        float purpleNoiseOffsetY = Random.Range(0f, 999f);
        float oceanNoiseOffsetX = Random.Range(0f, 999f);
        float oceanNoiseOffsetY = Random.Range(0f, 999f);

        float biomeScale = 0.18f;
        float oceanScale = 0.14f;

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                bool isNearBorder =
                    q <= 1 ||
                    r <= 1 ||
                    q >= width - 2 ||
                    r >= height - 2;

                float purpleNoise = Mathf.PerlinNoise(
                    purpleNoiseOffsetX + q * biomeScale,
                    purpleNoiseOffsetY + r * biomeScale
                );

                // Outer 2 rings are always land
                if (isNearBorder)
                {
                    plannedTypes[q, r] = purpleNoise < purpleChance
                        ? TileType.PURPLELAND
                        : TileType.GRASSLAND;
                    continue;
                }

                float oceanNoise = Mathf.PerlinNoise(
                    oceanNoiseOffsetX + q * oceanScale,
                    oceanNoiseOffsetY + r * oceanScale
                );

                if (oceanNoise < oceanChance)
                {
                    plannedTypes[q, r] = TileType.OCEAN_DEEP;
                    continue;
                }

                plannedTypes[q, r] = purpleNoise < purpleChance
                    ? TileType.PURPLELAND
                    : TileType.GRASSLAND;
            }
        }
    }

    private Tile CreateTile(int q, int r, TileType plannedType)
    {
        Tile tile = Instantiate(tilePrefab, transform);
        tile.axialPos = new Vector2Int(q, r);

        float xPos = hexSize * 1.5f * q;
        float yPos = hexSize * Mathf.Sqrt(3) * (r + 0.5f * (q % 2));
        tile.transform.localPosition = new Vector3(xPos, yPos, 0);

        tile.type = plannedType;
        ConfigureTileGameplay(tile);
        EnsureSelectionOutline(tile);

        return tile;
    }

    private void ConfigureTileGameplay(Tile tile)
    {
        switch (tile.type)
        {
            case TileType.GRASSLAND:
                tile.altitude = Random.value < 0.70f ? 0 : (Random.value < 0.85f ? 1 : 2);
                tile.passable = true;
                tile.moveCost = 1;
                break;

            case TileType.PURPLELAND:
                tile.altitude = Random.value < 0.70f ? 0 : (Random.value < 0.85f ? 1 : 2);
                tile.passable = true;
                tile.moveCost = 1;
                break;

            case TileType.SHORE:
                tile.altitude = 0;
                tile.passable = false;
                tile.moveCost = 999;
                break;

            case TileType.OCEAN_DEEP:
                tile.altitude = 0;
                tile.passable = false;
                tile.moveCost = 999;
                break;

            case TileType.MOUNTAIN:
                tile.altitude = 3;
                tile.passable = false;
                tile.moveCost = 999;
                break;
        }
    }

    private void EnsureSelectionOutline(Tile tile)
    {
        if (tile.selectionOutline != null)
            return;

        Transform existing = tile.transform.Find("SelectionOutline");
        GameObject outlineObj;

        if (existing != null)
        {
            outlineObj = existing.gameObject;
        }
        else
        {
            outlineObj = new GameObject("SelectionOutline");
            outlineObj.transform.SetParent(tile.transform, false);
        }

        SpriteRenderer outlineRenderer = outlineObj.GetComponent<SpriteRenderer>();
        if (outlineRenderer == null)
            outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();

        outlineRenderer.sprite = selectionOutlineSprite;
        outlineRenderer.color = Color.white;

        if (tile.spriteRenderer != null)
        {
            outlineRenderer.sortingLayerID = tile.spriteRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = tile.spriteRenderer.sortingOrder + 1;
        }

        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one;

        outlineObj.SetActive(false);
        tile.selectionOutline = outlineObj;
    }

    private void ApplyShorelines()
    {
        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                Tile tile = grid[q, r];
                if (tile == null || tile.type != TileType.OCEAN_DEEP)
                    continue;

                foreach (Tile neighbor in tile.neighbors)
                {
                    if (neighbor == null) continue;

                    if (neighbor.type == TileType.GRASSLAND ||
                        neighbor.type == TileType.PURPLELAND ||
                        neighbor.type == TileType.MOUNTAIN)
                    {
                        tile.type = TileType.SHORE;
                        break;
                    }
                }
            }
        }
    }

    private void RefreshAllTileVisuals()
    {
        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                Tile tile = grid[q, r];
                if (tile != null)
                    ApplyTileSprite(tile);
            }
        }
    }

    private void ApplyTileSprite(Tile tile)
    {
        if (tile.spriteRenderer == null)
        {
            Debug.LogError($"Tile({tile.axialPos.x},{tile.axialPos.y}): No SpriteRenderer!");
            return;
        }

        tile.spriteRenderer.color = Color.white;
        tile.spriteRenderer.sprite = GetSpriteForTile(tile);
    }

    private Sprite GetSpriteForTile(Tile tile)
    {
        switch (tile.type)
        {
            case TileType.GRASSLAND:
                return GetGrassSprite(tile.altitude);

            case TileType.PURPLELAND:
                return GetPurpleSprite();

            case TileType.SHORE:
                return shoreSprite != null ? shoreSprite : deepOceanSprite;

            case TileType.OCEAN_DEEP:
                return deepOceanSprite;

            case TileType.MOUNTAIN:
                return mountainSprite != null ? mountainSprite : grassRock2Sprite;

            default:
                return grassFlatSprite;
        }
    }

    private Sprite GetGrassSprite(int altitude)
    {
        if (altitude <= 0)
        {
            if (grassFlowerSprite != null && Random.value < grassVariantChance)
                return grassFlowerSprite;

            return grassFlatSprite;
        }

        if (altitude == 1)
            return grassRock1Sprite != null ? grassRock1Sprite : grassFlatSprite;

        return grassRock2Sprite != null ? grassRock2Sprite : grassRock1Sprite;
    }

    private Sprite GetPurpleSprite()
    {
        float roll = Random.value;

        if (purpleFlowerSprite != null && purpleMushroomSprite != null)
        {
            return roll < 0.5f ? purpleFlowerSprite : purpleMushroomSprite;
        }

        if (purpleFlowerSprite != null)
            return purpleFlowerSprite;

        if (purpleMushroomSprite != null)
            return purpleMushroomSprite;

        return grassFlatSprite;
    }

    private void LinkNeighbors()
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

    public Tile GetTileAt(Vector2Int coord)
    {
        if (coord.x < 0 || coord.x >= width || coord.y < 0 || coord.y >= height)
            return Tile.NullTile;

        return grid[coord.x, coord.y];
    }
}