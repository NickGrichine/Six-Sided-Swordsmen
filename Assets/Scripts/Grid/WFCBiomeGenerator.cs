using System.Collections.Generic;
using UnityEngine;

public static class WFCBiomeGenerator
{
    private class WfcCell
    {
        public HashSet<TileType> possible = new HashSet<TileType>();
        public bool IsCollapsed => possible.Count == 1;
    }

    private class LearnedRuleSet
    {
        public Dictionary<TileType, int> biomeCounts = new Dictionary<TileType, int>();
        public Dictionary<TileType, Dictionary<TileType, int>> adjacencyCounts =
            new Dictionary<TileType, Dictionary<TileType, int>>();

        public HashSet<TileType> allBiomes = new HashSet<TileType>();

        public void EnsureBiome(TileType type)
        {
            if (!biomeCounts.ContainsKey(type))
                biomeCounts[type] = 0;

            if (!adjacencyCounts.ContainsKey(type))
                adjacencyCounts[type] = new Dictionary<TileType, int>();

            allBiomes.Add(type);
        }

        public void AddBiome(TileType type)
        {
            EnsureBiome(type);
            biomeCounts[type]++;
        }

        public void AddAdjacency(TileType a, TileType b)
        {
            EnsureBiome(a);
            EnsureBiome(b);

            if (!adjacencyCounts[a].ContainsKey(b))
                adjacencyCounts[a][b] = 0;

            adjacencyCounts[a][b]++;
        }

        public int GetBiomeWeight(TileType type)
        {
            if (!biomeCounts.ContainsKey(type))
                return 1;

            return Mathf.Max(1, biomeCounts[type]);
        }

        public int GetAdjacencyWeight(TileType center, TileType neighbor)
        {
            if (!adjacencyCounts.ContainsKey(center))
                return 0;

            if (!adjacencyCounts[center].ContainsKey(neighbor))
                return 0;

            return adjacencyCounts[center][neighbor];
        }

        public bool IsCompatible(TileType a, TileType b)
        {
            return GetAdjacencyWeight(a, b) > 0 && GetAdjacencyWeight(b, a) > 0;
        }
    }

    private const int MaxAttempts = 20;

    public static void Generate(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        if (gridManager.biomeControlMask == null)
        {
            Debug.LogWarning("No example image assigned. Falling back to StaticBiomeGenerator.");
            StaticBiomeGenerator.Generate(gridManager, plannedTypes);
            return;
        }

        LearnedRuleSet rules = LearnRulesFromTexture(gridManager.biomeControlMask);

        if (rules.allBiomes.Count == 0)
        {
            Debug.LogWarning("Failed to learn rules from sample image. Falling back to StaticBiomeGenerator.");
            StaticBiomeGenerator.Generate(gridManager, plannedTypes);
            return;
        }

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (TryGenerate(gridManager, plannedTypes, rules))
            {
                Debug.Log($"Example-learned Hex WFC succeeded on attempt {attempt + 1}");
                return;
            }
        }

        Debug.LogWarning("Example-learned Hex WFC failed after all attempts. Falling back to StaticBiomeGenerator.");
        StaticBiomeGenerator.Generate(gridManager, plannedTypes);
    }

    private static bool TryGenerate(HexGridManager gridManager, TileType[,] plannedTypes, LearnedRuleSet rules)
    {
        int totalWidth = gridManager.TotalWidth;
        int totalHeight = gridManager.TotalHeight;
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        int playableMinQ = playableOffsetQ;
        int playableMaxQ = playableOffsetQ + width - 1;
        int playableMinR = playableOffsetR;
        int playableMaxR = playableOffsetR + height - 1;

        WfcCell[,] cells = new WfcCell[totalWidth, totalHeight];

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                cells[q, r] = new WfcCell();

                bool outsidePlayableArea =
                    q < playableMinQ || q > playableMaxQ ||
                    r < playableMinR || r > playableMaxR;

                if (outsidePlayableArea)
                {
                    cells[q, r].possible.Add(TileType.OCEAN_DEEP);
                    continue;
                }

                int localQ = q - playableOffsetQ;
                int localR = r - playableOffsetR;

                bool isNearInnerBorder =
                    localQ <= 1 ||
                    localR <= 1 ||
                    localQ >= width - 2 ||
                    localR >= height - 2;

                if (isNearInnerBorder)
                {
                    // Keep border safe/playable.
                    cells[q, r].possible.Add(TileType.GRASSLAND);
                    cells[q, r].possible.Add(TileType.PURPLELAND);
                }
                else
                {
                    foreach (TileType biome in rules.allBiomes)
                    {
                        cells[q, r].possible.Add(biome);
                    }

                    // Ensure core playable biomes are present even if sample is weird.
                    cells[q, r].possible.Add(TileType.GRASSLAND);
                    cells[q, r].possible.Add(TileType.PURPLELAND);
                    cells[q, r].possible.Add(TileType.MOUNTAIN);
                    cells[q, r].possible.Add(TileType.OCEAN_DEEP);
                }
            }
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                if (cells[q, r].IsCollapsed)
                    queue.Enqueue(new Vector2Int(q, r));
            }
        }

        if (!Propagate(cells, queue, totalWidth, totalHeight, rules))
            return false;

        while (true)
        {
            Vector2Int next = FindLowestEntropyCell(cells, totalWidth, totalHeight);

            if (next.x == -1)
                break;

            TileType chosen = ChooseWeightedTile(cells, next.x, next.y, totalWidth, totalHeight, rules);

            cells[next.x, next.y].possible.Clear();
            cells[next.x, next.y].possible.Add(chosen);

            queue.Enqueue(next);

            if (!Propagate(cells, queue, totalWidth, totalHeight, rules))
                return false;
        }

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                if (cells[q, r].possible.Count != 1)
                    return false;

                foreach (TileType t in cells[q, r].possible)
                {
                    plannedTypes[q, r] = t;
                    break;
                }
            }
        }

        return true;
    }

    private static LearnedRuleSet LearnRulesFromTexture(Texture2D texture)
    {
        LearnedRuleSet rules = new LearnedRuleSet();

        int width = texture.width;
        int height = texture.height;
        TileType[,] sample = new TileType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color c = texture.GetPixel(x, y);
                TileType t = ClassifySampleColor(c);
                sample[x, y] = t;
                rules.AddBiome(t);
            }
        }

        Vector2Int[] neighborDirsEven = new Vector2Int[]
        {
            new Vector2Int(+1, 0),
            new Vector2Int(0, +1),
            new Vector2Int(-1, +1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1)
        };

        Vector2Int[] neighborDirsOdd = new Vector2Int[]
        {
            new Vector2Int(+1, 0),
            new Vector2Int(+1, +1),
            new Vector2Int(0, +1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(+1, -1)
        };

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType center = sample[x, y];
                Vector2Int[] dirs = (x & 1) == 0 ? neighborDirsEven : neighborDirsOdd;

                foreach (Vector2Int d in dirs)
                {
                    int nx = x + d.x;
                    int ny = y + d.y;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    TileType neighbor = sample[nx, ny];
                    rules.AddAdjacency(center, neighbor);
                }
            }
        }

        return rules;
    }

    private static TileType ClassifySampleColor(Color c)
    {
        // Blue => water
        if (c.b > 0.75f && c.r < 0.45f && c.g < 0.65f)
            return TileType.OCEAN_DEEP;

        // Green => grass
        if (c.g > 0.75f && c.r < 0.55f && c.b < 0.55f)
            return TileType.GRASSLAND;

        // Magenta => purple
        if (c.r > 0.75f && c.b > 0.75f && c.g < 0.55f)
            return TileType.PURPLELAND;

        // Gray / white => mountain
        if (Mathf.Abs(c.r - c.g) < 0.08f &&
            Mathf.Abs(c.g - c.b) < 0.08f &&
            c.grayscale > 0.35f)
        {
            return TileType.MOUNTAIN;
        }

        // Default
        return TileType.GRASSLAND;
    }

    private static Vector2Int FindLowestEntropyCell(WfcCell[,] cells, int totalWidth, int totalHeight)
    {
        int bestCount = int.MaxValue;
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                int count = cells[q, r].possible.Count;
                if (count <= 1)
                    continue;

                if (count < bestCount)
                {
                    bestCount = count;
                    candidates.Clear();
                    candidates.Add(new Vector2Int(q, r));
                }
                else if (count == bestCount)
                {
                    candidates.Add(new Vector2Int(q, r));
                }
            }
        }

        if (candidates.Count == 0)
            return new Vector2Int(-1, -1);

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static TileType ChooseWeightedTile(
        WfcCell[,] cells,
        int q,
        int r,
        int totalWidth,
        int totalHeight,
        LearnedRuleSet rules)
    {
        HashSet<TileType> options = cells[q, r].possible;

        int totalWeight = 0;
        Dictionary<TileType, int> weights = new Dictionary<TileType, int>();

        foreach (TileType type in options)
        {
            int weight = rules.GetBiomeWeight(type);

            foreach (Vector2Int dir in HexMath.GetNeighborDirections(q))
            {
                int nq = q + dir.x;
                int nr = r + dir.y;

                if (nq < 0 || nq >= totalWidth || nr < 0 || nr >= totalHeight)
                    continue;

                if (cells[nq, nr].possible.Count == 1)
                {
                    TileType neighbor = GetSingleOption(cells[nq, nr]);
                    weight += rules.GetAdjacencyWeight(type, neighbor) * 2;
                }
            }

            weight = Mathf.Max(1, weight);
            weights[type] = weight;
            totalWeight += weight;
        }

        int roll = Random.Range(0, totalWeight);
        int running = 0;

        foreach (TileType type in options)
        {
            running += weights[type];
            if (roll < running)
                return type;
        }

        foreach (TileType type in options)
            return type;

        return TileType.GRASSLAND;
    }

    private static TileType GetSingleOption(WfcCell cell)
    {
        foreach (TileType t in cell.possible)
            return t;

        return TileType.GRASSLAND;
    }

    private static bool Propagate(
        WfcCell[,] cells,
        Queue<Vector2Int> queue,
        int totalWidth,
        int totalHeight,
        LearnedRuleSet rules)
    {
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            WfcCell currentCell = cells[current.x, current.y];

            foreach (Vector2Int dir in HexMath.GetNeighborDirections(current.x))
            {
                int nq = current.x + dir.x;
                int nr = current.y + dir.y;

                if (nq < 0 || nq >= totalWidth || nr < 0 || nr >= totalHeight)
                    continue;

                WfcCell neighbor = cells[nq, nr];
                HashSet<TileType> reduced = new HashSet<TileType>();

                foreach (TileType neighborOption in neighbor.possible)
                {
                    bool valid = false;

                    foreach (TileType currentOption in currentCell.possible)
                    {
                        if (rules.IsCompatible(currentOption, neighborOption))
                        {
                            valid = true;
                            break;
                        }
                    }

                    if (valid)
                        reduced.Add(neighborOption);
                }

                if (reduced.Count == 0)
                    return false;

                if (reduced.Count < neighbor.possible.Count)
                {
                    neighbor.possible = reduced;
                    queue.Enqueue(new Vector2Int(nq, nr));
                }
            }
        }

        return true;
    }
}