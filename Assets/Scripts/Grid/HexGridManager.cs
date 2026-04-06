using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public Tile tilePrefab; 
    public int width = 10;   // q cols
    public int height = 10;  // r rows
    public float hexSize = 1f; // Base Spacing factor for hex layout

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
    [SerializeField] private int oceanBorderThickness = 10;
    private int totalWidth;
    private int totalHeight;
    private int playableOffsetQ; // Left offset from total map to playable map in q
    private int playableOffsetR; // Top offset from total map to playable map in r

    [Header("Camera Bounds")]
    [SerializeField] private int cameraBorderTiles = 3;
    
    [Header("Generation Tuning")]
    [Range(0f, 1f)] public float oceanChance = 0.18f; // chance for tile to become ocean
    [Range(0f, 1f)] public float purpleChance = 0.32f; // chance for title to become purple
    [Range(0f, 0.5f)] public float grassVariantChance = 0.08f; // chance for grass tile to use flower variant
    [Range(0f, 1f)] public float mountainChance = 0.72f; // chance for tile to turn into mountain
    [Range(0.01f, 0.3f)] public float mountainScale = 0.09f; // Size of the noise pattern
    [Range(0f, 1f)] public float mountainBlendChance = 0.35f; // Chance for blending between mountain types 


    private Tile[,] grid;

    // public Fields

    public Tile[,] Grid => grid;

    public int TotalWidth => totalWidth;
    public int TotalHeight => totalHeight;
    public int PlayableOffsetQ => playableOffsetQ;
    public int PlayableOffsetR => playableOffsetR;
    public int OceanBorderThickness => oceanBorderThickness;
    public int CameraBorderTiles => cameraBorderTiles; 

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid(); // Clear old tiles

        // Outer ring offset for ocean border
        playableOffsetQ = oceanBorderThickness;
        playableOffsetR = oceanBorderThickness;

        // compute the size of the whole map
        totalWidth = width + oceanBorderThickness * 2;
        totalHeight = height + oceanBorderThickness * 2;

        // Stores tile & types for the grid
        grid = new Tile[totalWidth, totalHeight];
        TileType[,] plannedTypes = new TileType[totalWidth, totalHeight];

        // Fill plannedTypes with grassland/purpleland/deep ocean depending on rule & noise
        GenerateBiomePlan(plannedTypes);

        // Loops through every coordinate and create a tile there
        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                grid[q, r] = CreateTile(q, r, plannedTypes[q, r]);
            }
        }

        LinkNeighbors(); // Connect each tile to its adjacent hexes
        ApplyShorelines(); // Apply shoreline rules to deep ocean tiles touching land
        RefreshAllTileVisuals(); // Assign sprites to all tiles
        CenterCameraOnPlayableArea(); // Moves the camera to the middle of the playable region
        UpdateCameraBoundsToPlayableArea(); // Sets the camera bounds
    }

    private void GenerateBiomePlan(TileType[,] plannedTypes)
    {
        float purpleNoiseOffsetX = Random.Range(0f, 999f);
        float purpleNoiseOffsetY = Random.Range(0f, 999f);
        float oceanNoiseOffsetX = Random.Range(0f, 999f);
        float oceanNoiseOffsetY = Random.Range(0f, 999f);
        float mountainNoiseOffsetX = Random.Range(0f, 999f);
        float mountainNoiseOffsetY = Random.Range(0f, 999f);

        float biomeScale = 0.18f;
        float oceanScale = 0.14f;
        float mountainScaleLocal = mountainScale; // use inspector value

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

                if (outsidePlayableArea)
                {
                    plannedTypes[q, r] = TileType.OCEAN_DEEP;
                    continue;
                }

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

                float oceanNoise = Mathf.PerlinNoise(
                    oceanNoiseOffsetX + localQ * oceanScale,
                    oceanNoiseOffsetY + localR * oceanScale
                );

                float mountainNoise = Mathf.PerlinNoise(
                    mountainNoiseOffsetX + localQ * mountainScaleLocal,
                    mountainNoiseOffsetY + localR * mountainScaleLocal
                );

                // keep outer 2 rings as land and avoid mountains there
                if (isNearInnerBorder)
                {
                    plannedTypes[q, r] = purpleNoise < purpleChance
                        ? TileType.PURPLELAND
                        : TileType.GRASSLAND;
                    continue;
                }

                // ocean first
                if (oceanNoise < oceanChance)
                {
                    plannedTypes[q, r] = TileType.OCEAN_DEEP;
                    continue;
                }

                // mountain ranges / clumps
                if (mountainNoise > mountainChance)
                {
                    plannedTypes[q, r] = TileType.MOUNTAIN;
                    continue;
                }

                // optional fuzzy foothills around mountain ranges
                if (mountainNoise > mountainChance - 0.08f && Random.value < mountainBlendChance)
                {
                    plannedTypes[q, r] = TileType.MOUNTAIN;
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
        // Checks for camera instance
        if (CameraController.Instance == null)
            return;

        // Get the main camera
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        // Track world-space bounding box
        float minWorldX = float.MaxValue;
        float maxWorldX = float.MinValue;
        float minWorldY = float.MaxValue;
        float maxWorldY = float.MinValue;

        // Define rectangular bounds of the playable area inside full bordered map
        int boundedMinQ = Mathf.Max(0, playableOffsetQ - cameraBorderTiles);
        int boundedMaxQ = Mathf.Min(totalWidth - 1, playableOffsetQ + width - 1 + cameraBorderTiles);
        int boundedMinR = Mathf.Max(0, playableOffsetR - cameraBorderTiles);
        int boundedMaxR = Mathf.Min(totalHeight - 1, playableOffsetR + height - 1 + cameraBorderTiles);

        // Loops through only bounded region
        for (int q = boundedMinQ; q <= boundedMaxQ; q++)
        {
            for (int r = boundedMinR; r <= boundedMaxR; r++)
            {
                // Get each tile and its world-space position
                Tile tile = grid[q, r];
                if (tile == null) continue;

                Vector3 pos = tile.transform.position;

                // Track the extreme edges of the region
                if (pos.x < minWorldX) minWorldX = pos.x;
                if (pos.x > maxWorldX) maxWorldX = pos.x;
                if (pos.y < minWorldY) minWorldY = pos.y;
                if (pos.y > maxWorldY) maxWorldY = pos.y;
            }
        }

        // Define extra edge padding
        float tilePaddingLeft   = hexSize * 1.0f;
        float tilePaddingRight  = hexSize * 1.0f;
        float tilePaddingBottom = hexSize * 0.7f;
        float tilePaddingTop    = hexSize * 0.25f;

        // Expand raw bounds outwards
        minWorldX -= tilePaddingLeft;
        maxWorldX += tilePaddingRight;
        minWorldY -= tilePaddingBottom;
        maxWorldY += tilePaddingTop;

        // Half the visible size of the camera view
        float halfCameraHeight = cam.orthographicSize;
        float halfCameraWidth = cam.orthographicSize * cam.aspect;

        // Convert map-edge bounds into camera-center bounds
        float clampedMinX = minWorldX + halfCameraWidth;
        float clampedMaxX = maxWorldX - halfCameraWidth;
        float clampedMinY = minWorldY + halfCameraHeight;
        float clampedMaxY = maxWorldY - halfCameraHeight;

        // If the camera view is wider than allowed map region, collpase bounds to the center
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

        // Send the computed bounds to the camera controller
        CameraController.Instance.SetBounds(
            clampedMinX,
            clampedMaxX,
            clampedMinY,
            clampedMaxY
        );

        // Clamp the current camera position
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

                // Find world bounds
                if (pos.x < minWorldX) minWorldX = pos.x;
                if (pos.x > maxWorldX) maxWorldX = pos.x;
                if (pos.y < minWorldY) minWorldY = pos.y;
                if (pos.y > maxWorldY) maxWorldY = pos.y;
            }
        }

        // Set the camera position to the center of the bounds
        Vector3 camPos = cam.transform.position;
        camPos.x = (minWorldX + maxWorldX) * 0.5f;
        camPos.y = (minWorldY + maxWorldY) * 0.5f;
        cam.transform.position = camPos;
    }

    private Tile CreateTile(int q, int r, TileType plannedType)
    {
        // Clone the tile prefab as a child of the grid manager
        Tile tile = Instantiate(tilePrefab, transform);

        // Store grid coordinates
        tile.gridPos = new Vector2Int(q, r);

        // Assign tile ID
        tile.tileId = GetTileId(q, r);

        // Convert hex grid coordinates to world-space position
        float xPos = hexSize * 1.5f * q; // shifts right by 1.5 hex radii
        float yPos = hexSize * Mathf.Sqrt(3f) * (r + 0.5f * (q & 1));        
        
        // Position tile in the scene
        tile.transform.localPosition = new Vector3(xPos, yPos, 0);

        // Assign terrain type
        tile.type = plannedType;

        // Set altitude, passable, and move cost
        ConfigureTileGameplay(tile);

        // Ensure thin outline and selection outline
        EnsureSelectionOutline(tile);

        return tile;
    }

    private void ConfigureTileGameplay(Tile tile)
    {
        switch (tile.type)
        {
            case TileType.GRASSLAND:
                tile.altitude = Random.value < 0.82f ? 0 : 1;
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
                tile.altitude = 5;
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
        // Looks under tile GameObject for "BaseOutline"
        Transform existing = tile.transform.Find("BaseOutline");
        GameObject outlineObj;

        // If outline exist, use it
        if (existing != null)
        {
            outlineObj = existing.gameObject;
        }
        else // create new "BaseOutline" and make it a child of the tile
        {
            outlineObj = new GameObject("BaseOutline");
            outlineObj.transform.SetParent(tile.transform, false);
        }

        // Checks whether the outline object already has a SpriteRenderer
        SpriteRenderer outlineRenderer = outlineObj.GetComponent<SpriteRenderer>();
        if (outlineRenderer == null)
            outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();

        // Assign the base outline sprite
        outlineRenderer.sprite = baseOutlineSprite;
        outlineRenderer.color = Color.white;

        // Match the outline's sorting layer and order to the tile's
        if (tile.spriteRenderer != null)
        {
            outlineRenderer.sortingLayerID = tile.spriteRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = tile.spriteRenderer.sortingOrder + 1;
        }

        // Ensure it matches the tile's center
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one;

        // Turn base outline on
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
            outlineRenderer.sortingOrder = tile.spriteRenderer.sortingOrder + 2; // Make sure it on top of the base outline
        }

        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one;

        // Turn selection outline off
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
            Debug.LogError($"Tile({tile.gridPos.x},{tile.gridPos.y}): No SpriteRenderer!");
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
        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                Tile tile = grid[q, r];
                if (tile == null) continue;

                tile.ClearNeighbors();

                foreach (Vector2Int dir in HexMath.GetNeighborDirections(q))
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

    public int GetTileId(int q, int r)
    {
        return q * totalHeight + r;
    }

    public bool TryGetCoordFromTileId(int tileId, out int q, out int r)
    {
        q = -1;
        r = -1;

        if (tileId < 0 || totalHeight <= 0)
            return false;

        q = tileId / totalHeight;
        r = tileId % totalHeight;

        return q >= 0 && q < totalWidth && r >= 0 && r < totalHeight;
    }

    public Tile GetTileById(int tileId)
    {
        if (!TryGetCoordFromTileId(tileId, out int q, out int r))
            return Tile.NullTile;

        return grid[q, r];
    }

    public List<int> GetNeighborIds(Tile tile)
    {
        var ids = new List<int>();

        if (tile == null || tile.IsNull)
            return ids;

        foreach (Tile neighbor in tile.neighbors)
        {
            if (neighbor != null && !neighbor.IsNull)
                ids.Add(neighbor.tileId);
        }

        return ids;
    }

    public void RebuildNeighborsFromIds()
    {
        foreach (Tile tile in GetAllTiles())
        {
            if (tile == null || tile.IsNull) continue;
        
            tile.neighbors.Clear();

            foreach (int neighborId in tile.neighborIds)
            {
                Tile neighbor = GetTileById(neighborId);
                if (neighbor != null && !neighbor.IsNull && neighbor != tile)
                {
                    if (!tile.neighbors.Contains(neighbor))
                    {
                        tile.neighbors.Add(neighbor);
                    }
                }
            }
        }
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