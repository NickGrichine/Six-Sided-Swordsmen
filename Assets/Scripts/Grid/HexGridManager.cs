using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : Singleton<HexGridManager>
{
    [Header("Generation Mode")]
    [SerializeField] private bool generateOnStart = true;
    private bool hasLoadedFromSave = false;

    [Header("Grid Settings")]
    public Tile tilePrefab; 
    public int width = 10;   // q cols
    public int height = 10;  // r rows
    public float hexSize = 1f; // Base Spacing factor for hex layout

    [Header("Hex Layout Tuning")]
    [SerializeField] private float horizontalStepMultiplier = 1.48f;
    [SerializeField] private float verticalStepMultiplier = 1.73205f; // Mathf.Sqrt(3f)
    [SerializeField] private float oddColumnOffsetMultiplier = 0.50f;
    [SerializeField] private float horizontalNudge = 0f;
    [SerializeField] private float verticalNudge = 0f;

    public enum GenerationMode { Procedural, Static }

    [Header("Map Generation")]
    public GenerationMode generationMode = GenerationMode.Static;

    [Header("Procedural Control")]
    public Texture2D biomeControlMask;

    [Header("Biome Sprites")]
    public Sprite grassFlatSprite;      // Grass, no rocks
    public Sprite grassFlowerSprite;    // Grass, flowers

    public Sprite grassRock1Sprite;     // Grass, 1 rock
    public Sprite grassRock2Sprite;     // Grass, 3 rocks
    public Sprite grassRock3Sprite;     // Grass, 6 rocks

    public Sprite purpleFlowerSprite;   // Purple, flower
    public Sprite purpleMushroomSprite; // Purple, mushroom

    public Sprite shoreSprite;          // Shore
    public Sprite deepOceanSprite;      // Deep Ocean


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
    [Range(0f, 1f)] public float grassFlowerChance = 0.08f;

    // Mountain Altitude distribution
    [Range(0f, 1f)] public float mountainRock1Chance = 0.45f; // Altitude 1
    [Range(0f, 1f)] public float mountainRock2Chance = 0.35f; // Altitude 2

    // Mountain biome shape
    [Range(0f, 1f)] public float mountainChance = 0.74f; // chance for tile to turn into mountain
    [Range(0.01f, 0.3f)] public float mountainScale = 0.07f; // Size of the noise pattern
    [Range(0f, 1f)] public float mountainBlendChance = 0.18f; // Chance for blending between mountain types 

    private Tile[,] grid;

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
        ClearGrid();

        playableOffsetQ = oceanBorderThickness;
        playableOffsetR = oceanBorderThickness;

        totalWidth = width + oceanBorderThickness * 2;
        totalHeight = height + oceanBorderThickness * 2;

        grid = new Tile[totalWidth, totalHeight];
        TileType[,] plannedTypes = new TileType[totalWidth, totalHeight];

        GenerateBiomePlanByMode(plannedTypes);

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

    private void GenerateBiomePlanByMode(TileType[,] plannedTypes)
    {
        switch (generationMode)
        {
            case GenerationMode.Static:
                StaticBiomeGenerator.Generate(this, plannedTypes);
                break;

            case GenerationMode.Procedural:
                // TODO: implement WFC biome generation
                WFCBiomeGenerator.Generate(this, plannedTypes);
                break;

            default:
                StaticBiomeGenerator.Generate(this, plannedTypes);
                break;
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
                if (tile == null)
                    continue;

                Vector3 pos = tile.transform.position;

                if (pos.x < minWorldX) minWorldX = pos.x;
                if (pos.x > maxWorldX) maxWorldX = pos.x;
                if (pos.y < minWorldY) minWorldY = pos.y;
                if (pos.y > maxWorldY) maxWorldY = pos.y;
            }
        }

        float worldBorderX = oceanBorderThickness * hexSize * horizontalStepMultiplier;
        float worldBorderY = oceanBorderThickness * hexSize * verticalStepMultiplier * 0.5f;

        minWorldX -= worldBorderX;
        maxWorldX += worldBorderX;
        minWorldY -= worldBorderY;
        maxWorldY += worldBorderY;

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

        tile.gridPos = new Vector2Int(q, r);
        tile.tileId = GetTileId(q, r);

        float xPos = hexSize * horizontalStepMultiplier * q + horizontalNudge;
        float yPos = hexSize * verticalStepMultiplier * (r + oddColumnOffsetMultiplier * (q & 1)) + verticalNudge;

        tile.transform.localPosition = new Vector3(xPos, yPos, 0f);

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
                tile.altitude = 0;
                tile.passable = true;
                tile.moveCost = 1;
                tile.grassVariant = RollGrassVariant(); // 0 = flat, 1 = flower
                break;

            case TileType.PURPLELAND:
                tile.altitude = 0;
                tile.passable = true;
                tile.moveCost = 1;
                tile.grassVariant = 0;
                break;

            case TileType.SHORE:
                tile.altitude = 0;
                tile.passable = false;
                tile.moveCost = 999;
                tile.grassVariant = 0;
                break;

            case TileType.OCEAN_DEEP:
                tile.altitude = 0;
                tile.passable = false;
                tile.moveCost = 999;
                tile.grassVariant = 0;
                break;

            case TileType.MOUNTAIN:
                tile.altitude = RollMountainAltitude();
                tile.passable = true;
                tile.moveCost = 2;
                tile.grassVariant = 0;
                break;
        }
    }

    private int RollGrassVariant()
    {
        return Random.value < grassFlowerChance ? 1 : 0;
    }

    private int RollMountainAltitude()
    {
        float roll = Random.value;

        if (roll < mountainRock1Chance)
            return 1; // 1 rock
        
        roll -= mountainRock1Chance;

        if (roll < mountainRock2Chance)
            return 2;

        return 3;
    }

    private void EnsureSelectionOutline(Tile tile)
    {
        EnsureBaseOutline(tile);
        EnsureSelectedOutline(tile);
        EnsureCommandHighlight(tile);
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
        outlineObj.transform.localScale = new Vector3(1.01f, 1.01f, 1f); // 1.03 - 1.06 would work

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
                return GetGrassSprite(tile);

            case TileType.PURPLELAND:
                return GetPurpleSprite();

            case TileType.SHORE:
                return shoreSprite != null ? shoreSprite : deepOceanSprite;

            case TileType.OCEAN_DEEP:
                return deepOceanSprite;

            case TileType.MOUNTAIN:
                return GetMountainSprite(tile);

            default:
                return grassFlatSprite;
        }
    }

    private Sprite GetGrassSprite(Tile tile)
    {
        return tile.grassVariant == 1 && grassFlowerSprite != null
            ? grassFlowerSprite
            : grassFlatSprite;    
    }

    private Sprite GetMountainSprite(Tile tile)
    {
        switch (tile.altitude)
        {
            case 1:
                return grassRock1Sprite != null ? grassRock1Sprite : grassFlatSprite;

            case 2:
                return grassRock2Sprite != null
                    ? grassRock2Sprite
                    : (grassRock1Sprite != null ? grassRock1Sprite : grassFlatSprite);

            case 3:
                return grassRock3Sprite != null
                    ? grassRock3Sprite
                    : (grassRock2Sprite != null
                        ? grassRock2Sprite
                        : (grassRock1Sprite != null ? grassRock1Sprite : grassFlatSprite));
            default:
                return grassFlatSprite;
            }    
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

    void Awake()
    {
        base.Awake();
        
        if (generateOnStart && ! hasLoadedFromSave)
        {
    
            GenerateGrid();
        }
    
    }

    public void MarkLoadedFromSave()
    {
        hasLoadedFromSave = true;
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
            return null;

        return grid[q, r];
    }

    public List<int> GetNeighborIds(Tile tile)
    {
        var ids = new List<int>();

        if (tile == null)
            return ids;

        foreach (Tile neighbor in tile.neighbors)
        {
            if (neighbor != null)
                ids.Add(neighbor.tileId);
        }

        return ids;
    }

    public void RebuildNeighborsFromIds()
    {
        foreach (Tile tile in GetAllTiles())
        {
            if (tile == null) continue;

            tile.neighbors.Clear();

            foreach (int neighborId in tile.neighborIds)
            {
                Tile neighbor = GetTileById(neighborId);
                if (neighbor != null && neighbor != tile)
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
            return null;

        return grid[q, r];
    }

    public void RestoreGridSettings(int width, int height, float hexSize, int oceanBorderThickness, int cameraBorderTiles)
    {
        this.width = width;
        this.height = height;
        this.hexSize = hexSize;
        this.oceanBorderThickness = oceanBorderThickness;
        this.cameraBorderTiles = cameraBorderTiles;
    }

    public void RefreshAfterLoad()
    {
        RefreshAllTileVisuals();
        CenterCameraOnPlayableArea();
        UpdateCameraBoundsToPlayableArea();
    }

    // Command Highlighting
    private void EnsureCommandHighlight(Tile tile)
    {
        if (tile.commandHighlight != null)
            return;

        Transform existing = tile.transform.Find("CommandHighlight");
        GameObject outlineObj;

        if (existing != null)
        {
            outlineObj = existing.gameObject;
        }
        else
        {
            outlineObj = new GameObject("CommandHighlight");
            outlineObj.transform.SetParent(tile.transform, false);
        }

        SpriteRenderer outlineRenderer = outlineObj.GetComponent<SpriteRenderer>();
        if (outlineRenderer == null)
            outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();

        outlineRenderer.sprite = selectionOutlineSprite;
        outlineRenderer.color = Color.cyan;

        if (tile.spriteRenderer != null)
        {
            outlineRenderer.sortingLayerID = tile.spriteRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = tile.spriteRenderer.sortingOrder + 3;
        }

        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = new Vector3(1.03f, 1.03f, 1f);

        outlineObj.SetActive(false);
        tile.commandHighlight = outlineObj;
    }

    public void ClearAllCommandHighlights()
    {
        foreach (Tile tile in GetAllTiles())
        {
            if (tile != null)
            {
                tile.HideCommandHighlight();
            }
        }
    }

    public void ShowCommandHighlights(IEnumerable<Tile> tiles)
    {
        ClearAllCommandHighlights();

        foreach (Tile tile in tiles)
        {
            if (tile != null)
                tile.ShowCommandHighlight();
        }
    }

    public HashSet<Tile> GetValidMoveTiles(UnitController actor)
    {
        HashSet<Tile> result = new HashSet<Tile>();

        if (actor == null || actor.position == null)
            return result;

        Tile start = actor.position;
        int maxCost = actor.movesRemaining;

        Dictionary<Tile, int> bestCost = new Dictionary<Tile, int>();
        Queue<Tile> frontier = new Queue<Tile>();

        bestCost[start] = 0;
        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            Tile current = frontier.Dequeue();
            int currentCost = bestCost[current];

            foreach (Tile neighbor in current.neighbors)
            {
                if (neighbor == null)
                    continue;

                if (!neighbor.passable)
                    continue;

                if (neighbor.IsOccupied)
                    continue;

                if (!neighbor.CanClimbFrom(current))
                    continue;

                int nextCost = currentCost + neighbor.moveCost;
                if (nextCost > maxCost)
                    continue;

                if (!bestCost.ContainsKey(neighbor) || nextCost < bestCost[neighbor])
                {
                    bestCost[neighbor] = nextCost;
                    frontier.Enqueue(neighbor);
                    result.Add(neighbor);
                }
            }
        }

        result.Remove(start);
        return result;
    }

    public HashSet<Tile> GetValidAttackTiles(UnitController actor)
    {
        HashSet<Tile> result = new HashSet<Tile>();

        if (actor == null || actor.position == null)
            return result;

        foreach (Tile tile in GetAllTiles())
        {
            if (tile == null || !tile.IsOccupied)
                continue;

            UnitController target = tile.occupant as UnitController;
            if (target == null)
                continue;

            if (CombatUtils.CanAttack(actor, target))
                result.Add(tile);
        }

        return result;
    }

    public HashSet<Tile> GetValidTilesForCommand(UnitController actor, UnitCommandSO command)
    {
        HashSet<Tile> result = new HashSet<Tile>();

        if (actor == null || command == null)
            return result;

        switch (command.category)
        {
            case CommandCategory.Move:
                return GetValidMoveTiles(actor);

            case CommandCategory.Attack:
                return GetValidAttackTiles(actor);

            default:
                return result;
        }
    }

    public void ClearAllOccupants()
    {
        foreach (Tile tile in GetAllTiles())
        {
            if (tile != null)
            {
                tile.occupant = null;
            }
        }
    }

}