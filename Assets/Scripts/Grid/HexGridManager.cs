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
    public Sprite baseOutlineSprite; // Thin border
    public Sprite selectionOutlineSprite; // Thick border, selected

    [Header("Ocean Border")]
    [SerializeField] private int oceanBorderThickness = 5;
    private int totalWidth;
    private int totalHeight;
    private int playableOffsetQ;
    private int playableOffsetR;

    [Header("Camera Bounds")]
    [SerializeField] private int cameraBorderTiles = 3;
    
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

        playableOffsetQ = oceanBorderThickness;
        playableOffsetR = oceanBorderThickness;

        totalWidth = width + oceanBorderThickness * 2;
        totalHeight = height + oceanBorderThickness * 2;

        grid = new Tile[totalWidth, totalHeight];
        TileType[,] plannedTypes = new TileType[totalWidth, totalHeight];

        GenerateBiomePlan(plannedTypes);

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                grid[q, r] = CreateTile(q, r, plannedTypes[q, r]);
            }
        }

        LinkNeighbors();
        ApplyShorelines();
        RefreshAllTileVisuals();
        CenterCameraOnPlayableArea();
        UpdateCameraBoundsToPlayableArea();
    }

    private void GenerateBiomePlan(TileType[,] plannedTypes)
    {
        float purpleNoiseOffsetX = Random.Range(0f, 999f);
        float purpleNoiseOffsetY = Random.Range(0f, 999f);
        float oceanNoiseOffsetX = Random.Range(0f, 999f);
        float oceanNoiseOffsetY = Random.Range(0f, 999f);

        float biomeScale = 0.18f;
        float oceanScale = 0.14f;

        int playableMinQ = playableOffsetQ;
        int playableMaxQ = playableOffsetQ + width - 1;
        int playableMinR = playableOffsetR;
        int playableMaxR = playableOffsetR + height - 1;

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                bool outsidePlayableArea =
                    q < playableMinQ ||
                    q > playableMaxQ ||
                    r < playableMinR ||
                    r > playableMaxR;

                // Entire outer border area is forced ocean
                if (outsidePlayableArea)
                {
                    plannedTypes[q, r] = TileType.OCEAN_DEEP;
                    continue;
                }

                // Local coordinates inside the playable area
                int localQ = q - playableOffsetQ;
                int localR = r - playableOffsetR;

                bool isNearInnerBorder =
                    localQ <= 1 ||
                    localR <= 1 ||
                    localQ >= width - 2 ||
                    localR >= height - 2;

                float purpleNoise = Mathf.PerlinNoise(
                    purpleNoiseOffsetX + localQ * biomeScale,
                    purpleNoiseOffsetY + localR * biomeScale
                );

                // Keep outer 2 rings of the playable area as land
                if (isNearInnerBorder)
                {
                    plannedTypes[q, r] = purpleNoise < purpleChance
                        ? TileType.PURPLELAND
                        : TileType.GRASSLAND;
                    continue;
                }

                float oceanNoise = Mathf.PerlinNoise(
                    oceanNoiseOffsetX + localQ * oceanScale,
                    oceanNoiseOffsetY + localR * oceanScale
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

    private void UpdateCameraBoundsToPlayableArea()
    {
        if (CameraController.Instance == null)
            return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        float minWorldX = float.MaxValue;
        float maxWorldX = float.MinValue;
        float minWorldY = float.MaxValue;
        float maxWorldY = float.MinValue;

        int boundedMinQ = Mathf.Max(0, playableOffsetQ - cameraBorderTiles);
        int boundedMaxQ = Mathf.Min(totalWidth - 1, playableOffsetQ + width - 1 + cameraBorderTiles);
        int boundedMinR = Mathf.Max(0, playableOffsetR - cameraBorderTiles);
        int boundedMaxR = Mathf.Min(totalHeight - 1, playableOffsetR + height - 1 + cameraBorderTiles);

        for (int q = boundedMinQ; q <= boundedMaxQ; q++)
        {
            for (int r = boundedMinR; r <= boundedMaxR; r++)
            {
                Tile tile = grid[q, r];
                if (tile == null) continue;

                Vector3 pos = tile.transform.position;

                if (pos.x < minWorldX) minWorldX = pos.x;
                if (pos.x > maxWorldX) maxWorldX = pos.x;
                if (pos.y < minWorldY) minWorldY = pos.y;
                if (pos.y > maxWorldY) maxWorldY = pos.y;
            }
        }

        float tilePaddingLeft   = hexSize * 1.0f;
        float tilePaddingRight  = hexSize * 1.0f;
        float tilePaddingBottom = hexSize * 0.7f;
        float tilePaddingTop    = hexSize * 0.25f;

        minWorldX -= tilePaddingLeft;
        maxWorldX += tilePaddingRight;
        minWorldY -= tilePaddingBottom;
        maxWorldY += tilePaddingTop;

        float halfCameraHeight = cam.orthographicSize;
        float halfCameraWidth = cam.orthographicSize * cam.aspect;

        float clampedMinX = minWorldX + halfCameraWidth;
        float clampedMaxX = maxWorldX - halfCameraWidth;
        float clampedMinY = minWorldY + halfCameraHeight;
        float clampedMaxY = maxWorldY - halfCameraHeight;

        if (clampedMinX > clampedMaxX)
        {
            float centerX = (minWorldX + maxWorldX) * 0.5f;
            clampedMinX = centerX;
            clampedMaxX = centerX;
        }

        if (clampedMinY > clampedMaxY)
        {
            float centerY = (minWorldY + maxWorldY) * 0.5f;
            clampedMinY = centerY;
            clampedMaxY = centerY;
        }

        CameraController.Instance.SetBounds(
            clampedMinX,
            clampedMaxX,
            clampedMinY,
            clampedMaxY
        );

        Vector3 camPos = cam.transform.position;
        camPos.x = Mathf.Clamp(camPos.x, clampedMinX, clampedMaxX);
        camPos.y = Mathf.Clamp(camPos.y, clampedMinY, clampedMaxY);
        cam.transform.position = camPos;
    }

    private void CenterCameraOnPlayableArea()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        float minWorldX = float.MaxValue;
        float maxWorldX = float.MinValue;
        float minWorldY = float.MaxValue;
        float maxWorldY = float.MinValue;

        int boundedMinQ = Mathf.Max(0, playableOffsetQ - cameraBorderTiles);
        int boundedMaxQ = Mathf.Min(totalWidth - 1, playableOffsetQ + width - 1 + cameraBorderTiles);
        int boundedMinR = Mathf.Max(0, playableOffsetR - cameraBorderTiles);
        int boundedMaxR = Mathf.Min(totalHeight - 1, playableOffsetR + height - 1 + cameraBorderTiles);

        for (int q = boundedMinQ; q <= boundedMaxQ; q++)
        {
            for (int r = boundedMinR; r <= boundedMaxR; r++)
            {
                Tile tile = grid[q, r];
                if (tile == null) continue;

                Vector3 pos = tile.transform.position;

                if (pos.x < minWorldX) minWorldX = pos.x;
                if (pos.x > maxWorldX) maxWorldX = pos.x;
                if (pos.y < minWorldY) minWorldY = pos.y;
                if (pos.y > maxWorldY) maxWorldY = pos.y;
            }
        }

        Vector3 camPos = cam.transform.position;
        camPos.x = (minWorldX + maxWorldX) * 0.5f;
        camPos.y = (minWorldY + maxWorldY) * 0.5f;
        cam.transform.position = camPos;
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
        EnsureBaseOutline(tile);
        EnsureSelectedOutline(tile);
    }

    private void EnsureBaseOutline(Tile tile)
    {
        Transform existing = tile.transform.Find("BaseOutline");
        GameObject outlineObj;

        if (existing != null)
        {
            outlineObj = existing.gameObject;
        }
        else
        {
            outlineObj = new GameObject("BaseOutline");
            outlineObj.transform.SetParent(tile.transform, false);
        }

        SpriteRenderer outlineRenderer = outlineObj.GetComponent<SpriteRenderer>();
        if (outlineRenderer == null)
            outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();

        outlineRenderer.sprite = baseOutlineSprite;
        outlineRenderer.color = Color.white;

        if (tile.spriteRenderer != null)
        {
            outlineRenderer.sortingLayerID = tile.spriteRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = tile.spriteRenderer.sortingOrder + 1;
        }

        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one;

        outlineObj.SetActive(true);
    }

    private void EnsureSelectedOutline(Tile tile)
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
            outlineRenderer.sortingOrder = tile.spriteRenderer.sortingOrder + 2;
        }

        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one;

        outlineObj.SetActive(false);
        tile.selectionOutline = outlineObj;
    }

    private void ApplyShorelines()
    {
        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
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
        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
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

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                Tile tile = grid[q, r];
                foreach (Vector2Int dir in directions)
                {
                    int nq = q + dir.x;
                    int nr = r + dir.y;
                    if (nq >= 0 && nq < totalWidth && nr >= 0 && nr < totalHeight)
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
        for (int q = 0; q < totalWidth; q++)
            for (int r = 0; r < totalHeight; r++)
                if (grid[q, r] != null)
                    yield return grid[q, r];
    }

    public Tile GetTileAt(Vector2Int coord)
    {
        int q = coord.x + playableOffsetQ;
        int r = coord.y + playableOffsetR;

        if (q < 0 || q >= totalWidth || r < 0 || r >= totalHeight)
            return Tile.NullTile;

        return grid[q, r];
    }
}