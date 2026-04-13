using System;
using System.Collections.Generic;
using UnityEngine;

public static class WFCBiomeGenerator
{
    // Hex adaptation of overlapping-pattern WFC:
    // each wave cell is a superposition of pattern IDs, not tile types.

    private const int PatternRadius = 1;   // 1 = 7-cell hex pattern, 2 = 19-cell pattern
    private const bool UseRotations = true;
    private const int MaxAttempts = 20;

    // Cube directions for hex neighbors
    private static readonly Vector3Int[] CubeDirs =
    {
        new Vector3Int(+1, -1,  0),
        new Vector3Int(+1,  0, -1),
        new Vector3Int( 0, +1, -1),
        new Vector3Int(-1, +1,  0),
        new Vector3Int(-1,  0, +1),
        new Vector3Int( 0, -1, +1)
    };

    private class Pattern
    {
        public TileType[] values;   // aligned to footprint
        public int weight;          // frequency from sample
    }

    public static void Generate(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        if (gridManager.biomeControlMask == null)
        {
            Debug.LogWarning("No example image assigned. Falling back to StaticBiomeGenerator.");
            StaticBiomeGenerator.Generate(gridManager, plannedTypes);
            return;
        }

        List<Vector3Int> footprint = BuildHexFootprint(PatternRadius);
        Dictionary<Vector3Int, int> footprintIndex = BuildFootprintIndex(footprint);
        int centerIndex = footprintIndex[Vector3Int.zero];

        TileType[,] sample = BuildSampleBiomeMap(gridManager.biomeControlMask);
        DebugSampleCounts(sample);

        List<Pattern> patterns = ExtractPatterns(sample, footprint, footprintIndex);
        if (patterns.Count == 0)
        {
            Debug.LogWarning("No valid overlapping patterns extracted. Falling back to StaticBiomeGenerator.");
            StaticBiomeGenerator.Generate(gridManager, plannedTypes);
            return;
        }

        List<int>[,] compatibility = BuildCompatibility(patterns, footprint, footprintIndex);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (TryGenerate(gridManager, plannedTypes, patterns, compatibility, centerIndex))
            {
                Debug.Log($"Hex overlapping-pattern WFC succeeded on attempt {attempt + 1}");
                return;
            }
        }

        Debug.LogWarning("Hex overlapping-pattern WFC failed after all attempts. Falling back to StaticBiomeGenerator.");
        StaticBiomeGenerator.Generate(gridManager, plannedTypes);
    }

    private static bool TryGenerate(
        HexGridManager gridManager,
        TileType[,] plannedTypes,
        List<Pattern> patterns,
        List<int>[,] compatibility,
        int centerIndex)
    {
        int totalWidth = gridManager.TotalWidth;
        int totalHeight = gridManager.TotalHeight;
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;
        int patternCount = patterns.Count;

        int playableMinQ = playableOffsetQ;
        int playableMaxQ = playableOffsetQ + width - 1;
        int playableMinR = playableOffsetR;
        int playableMaxR = playableOffsetR + height - 1;

        HashSet<int>[,] wave = new HashSet<int>[totalWidth, totalHeight];

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                wave[q, r] = new HashSet<int>();

                bool outsidePlayableArea =
                    q < playableMinQ || q > playableMaxQ ||
                    r < playableMinR || r > playableMaxR;

                if (outsidePlayableArea)
                {
                    // Force ocean border
                    for (int p = 0; p < patternCount; p++)
                    {
                        if (patterns[p].values[centerIndex] == TileType.OCEAN_DEEP)
                            wave[q, r].Add(p);
                    }

                    if (wave[q, r].Count == 0)
                        return false;

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
                    // Keep the playable edge friendly, but allow water if learned
                    for (int p = 0; p < patternCount; p++)
                    {
                        TileType center = patterns[p].values[centerIndex];
                        if (center != TileType.MOUNTAIN)
                            wave[q, r].Add(p);
                    }

                    if (wave[q, r].Count == 0)
                        return false;
                }
                else
                {
                    for (int p = 0; p < patternCount; p++)
                        wave[q, r].Add(p);
                }
            }
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                if (wave[q, r].Count == 1)
                    queue.Enqueue(new Vector2Int(q, r));
            }
        }

        if (!Propagate(wave, queue, totalWidth, totalHeight, compatibility))
            return false;

        while (true)
        {
            Vector2Int next = FindLowestEntropyCell(wave, totalWidth, totalHeight, patterns);

            if (next.x == -1)
                break;

            int chosen = ChooseWeightedPattern(wave[next.x, next.y], patterns, centerIndex);
            wave[next.x, next.y].Clear();
            wave[next.x, next.y].Add(chosen);

            queue.Enqueue(next);

            if (!Propagate(wave, queue, totalWidth, totalHeight, compatibility))
                return false;
        }

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                if (wave[q, r].Count == 0)
                    return false;

                int patternId = First(wave[q, r]);
                plannedTypes[q, r] = patterns[patternId].values[centerIndex];
            }
        }

        return true;
    }

    private static TileType[,] BuildSampleBiomeMap(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        TileType[,] sample = new TileType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                sample[x, y] = ClassifySampleColor(texture.GetPixel(x, y));
            }
        }

        return sample;
    }

    private static List<Pattern> ExtractPatterns(
        TileType[,] sample,
        List<Vector3Int> footprint,
        Dictionary<Vector3Int, int> footprintIndex)
    {
        int sampleWidth = sample.GetLength(0);
        int sampleHeight = sample.GetLength(1);

        Dictionary<string, Pattern> unique = new Dictionary<string, Pattern>();

        for (int q = 0; q < sampleWidth; q++)
        {
            for (int r = 0; r < sampleHeight; r++)
            {
                if (!TryBuildPattern(sample, q, r, footprint, out TileType[] values))
                    continue;

                AddPattern(unique, values);

                if (UseRotations)
                {
                    TileType[] rotated = values;
                    for (int i = 0; i < 5; i++)
                    {
                        rotated = RotatePattern(rotated, footprint, footprintIndex);
                        AddPattern(unique, rotated);
                    }
                }
            }
        }

        return new List<Pattern>(unique.Values);
    }

    private static bool TryBuildPattern(
        TileType[,] sample,
        int centerQ,
        int centerR,
        List<Vector3Int> footprint,
        out TileType[] values)
    {
        int sampleWidth = sample.GetLength(0);
        int sampleHeight = sample.GetLength(1);

        values = new TileType[footprint.Count];
        Vector3Int centerCube = HexMath.OddQToCube(new Vector2Int(centerQ, centerR));

        for (int i = 0; i < footprint.Count; i++)
        {
            Vector3Int worldCube = centerCube + footprint[i];
            Vector2Int odd = HexMath.CubeToOddQ(worldCube);

            if (odd.x < 0 || odd.x >= sampleWidth || odd.y < 0 || odd.y >= sampleHeight)
                return false;

            values[i] = sample[odd.x, odd.y];
        }

        return true;
    }

    private static void AddPattern(Dictionary<string, Pattern> unique, TileType[] values)
    {
        string key = BuildPatternKey(values);

        if (unique.TryGetValue(key, out Pattern existing))
        {
            existing.weight++;
            return;
        }

        unique[key] = new Pattern
        {
            values = (TileType[])values.Clone(),
            weight = 1
        };
    }

    private static string BuildPatternKey(TileType[] values)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(values.Length * 2);
        for (int i = 0; i < values.Length; i++)
        {
            sb.Append((int)values[i]);
            sb.Append(',');
        }
        return sb.ToString();
    }

    private static TileType[] RotatePattern(
        TileType[] original,
        List<Vector3Int> footprint,
        Dictionary<Vector3Int, int> footprintIndex)
    {
        TileType[] rotated = new TileType[original.Length];

        for (int i = 0; i < footprint.Count; i++)
        {
            Vector3Int oldOffset = footprint[i];
            Vector3Int newOffset = RotateCube60(oldOffset);
            int newIndex = footprintIndex[newOffset];
            rotated[newIndex] = original[i];
        }

        return rotated;
    }

    private static Vector3Int RotateCube60(Vector3Int c)
    {
        // 60° rotation around origin in cube space
        return new Vector3Int(-c.z, -c.x, -c.y);
    }

    private static List<int>[,] BuildCompatibility(
        List<Pattern> patterns,
        List<Vector3Int> footprint,
        Dictionary<Vector3Int, int> footprintIndex)
    {
        int patternCount = patterns.Count;
        int dirCount = CubeDirs.Length;
        List<int>[,] compatibility = new List<int>[dirCount, patternCount];

        for (int dir = 0; dir < dirCount; dir++)
        {
            for (int p = 0; p < patternCount; p++)
            {
                compatibility[dir, p] = new List<int>();
            }
        }

        for (int dir = 0; dir < dirCount; dir++)
        {
            Vector3Int shift = CubeDirs[dir];

            for (int a = 0; a < patternCount; a++)
            {
                for (int b = 0; b < patternCount; b++)
                {
                    if (PatternsOverlapCompatibly(patterns[a], patterns[b], footprint, footprintIndex, shift))
                    {
                        compatibility[dir, a].Add(b);
                    }
                }
            }
        }

        return compatibility;
    }

    private static bool PatternsOverlapCompatibly(
        Pattern a,
        Pattern b,
        List<Vector3Int> footprint,
        Dictionary<Vector3Int, int> footprintIndex,
        Vector3Int shift)
    {
        for (int i = 0; i < footprint.Count; i++)
        {
            Vector3Int offsetA = footprint[i];
            Vector3Int offsetB = offsetA - shift;

            if (!footprintIndex.TryGetValue(offsetB, out int j))
                continue;

            if (a.values[i] != b.values[j])
                return false;
        }

        return true;
    }

    private static bool Propagate(
        HashSet<int>[,] wave,
        Queue<Vector2Int> queue,
        int totalWidth,
        int totalHeight,
        List<int>[,] compatibility)
    {
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Vector3Int currentCube = HexMath.OddQToCube(current);

            for (int dir = 0; dir < CubeDirs.Length; dir++)
            {
                Vector3Int neighborCube = currentCube + CubeDirs[dir];
                Vector2Int neighborOdd = HexMath.CubeToOddQ(neighborCube);

                int nq = neighborOdd.x;
                int nr = neighborOdd.y;

                if (nq < 0 || nq >= totalWidth || nr < 0 || nr >= totalHeight)
                    continue;

                HashSet<int> currentDomain = wave[current.x, current.y];
                HashSet<int> neighborDomain = wave[nq, nr];

                HashSet<int> reduced = new HashSet<int>();

                foreach (int neighborPattern in neighborDomain)
                {
                    bool allowed = false;

                    foreach (int currentPattern in currentDomain)
                    {
                        if (compatibility[dir, currentPattern].Contains(neighborPattern))
                        {
                            allowed = true;
                            break;
                        }
                    }

                    if (allowed)
                        reduced.Add(neighborPattern);
                }

                if (reduced.Count == 0)
                    return false;

                if (reduced.Count < neighborDomain.Count)
                {
                    wave[nq, nr] = reduced;
                    queue.Enqueue(new Vector2Int(nq, nr));
                }
            }
        }

        return true;
    }

    private static Vector2Int FindLowestEntropyCell(
        HashSet<int>[,] wave,
        int totalWidth,
        int totalHeight,
        List<Pattern> patterns)
    {
        float bestEntropy = float.PositiveInfinity;
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                HashSet<int> domain = wave[q, r];
                if (domain.Count <= 1)
                    continue;

                float entropy = ComputeEntropy(domain, patterns);

                if (entropy < bestEntropy - 0.0001f)
                {
                    bestEntropy = entropy;
                    candidates.Clear();
                    candidates.Add(new Vector2Int(q, r));
                }
                else if (Mathf.Abs(entropy - bestEntropy) < 0.0001f)
                {
                    candidates.Add(new Vector2Int(q, r));
                }
            }
        }

        if (candidates.Count == 0)
            return new Vector2Int(-1, -1);

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static float ComputeEntropy(HashSet<int> domain, List<Pattern> patterns)
    {
        float totalWeight = 0f;
        float weightedLogSum = 0f;

        foreach (int p in domain)
        {
            float w = Mathf.Max(1, patterns[p].weight);
            totalWeight += w;
            weightedLogSum += w * Mathf.Log(w);
        }

        if (totalWeight <= 0f)
            return 0f;

        float entropy = Mathf.Log(totalWeight) - (weightedLogSum / totalWeight);

        // tiny noise to reduce tie patterns like mxgmn
        entropy += UnityEngine.Random.value * 0.0001f;
        return entropy;
    }

    private static int ChooseWeightedPattern(HashSet<int> domain, List<Pattern> patterns, int centerIndex)
    {
        int totalWeight = 0;
        Dictionary<int, int> weights = new Dictionary<int, int>();

        foreach (int p in domain)
        {
            int weight = Mathf.Max(1, patterns[p].weight);

            // Slightly favor water-centered patterns
            if (patterns[p].values[centerIndex] == TileType.OCEAN_DEEP)
                weight *= 2;

            weights[p] = weight;
            totalWeight += weight;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int running = 0;

        foreach (int p in domain)
        {
            running += weights[p];
            if (roll < running)
                return p;
        }

        return First(domain);
    }

    private static int First(HashSet<int> set)
    {
        foreach (int v in set)
            return v;

        return -1;
    }

    private static List<Vector3Int> BuildHexFootprint(int radius)
    {
        List<Vector3Int> offsets = new List<Vector3Int>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int z = -x - y;
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= radius)
                {
                    offsets.Add(new Vector3Int(x, y, z));
                }
            }
        }

        offsets.Sort((a, b) =>
        {
            int da = CubeDistance(a);
            int db = CubeDistance(b);
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });

        return offsets;
    }

    private static Dictionary<Vector3Int, int> BuildFootprintIndex(List<Vector3Int> footprint)
    {
        Dictionary<Vector3Int, int> map = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < footprint.Count; i++)
            map[footprint[i]] = i;
        return map;
    }

    private static int CubeDistance(Vector3Int c)
    {
        return Mathf.Max(Mathf.Abs(c.x), Mathf.Abs(c.y), Mathf.Abs(c.z));
    }

    private static TileType ClassifySampleColor(Color c)
    {
        // Cyan / blue water
        if (c.b > 0.70f && c.g > 0.45f && c.r < 0.25f)
            return TileType.OCEAN_DEEP;

        // Darker blue fallback
        if (c.b > 0.65f && c.r < 0.35f && c.g < 0.80f)
            return TileType.OCEAN_DEEP;

        // Green grass
        if (c.g > 0.60f && c.r < 0.45f && c.b < 0.45f)
            return TileType.GRASSLAND;

        // Purple / magenta
        if (c.r > 0.35f && c.b > 0.45f && c.g < 0.35f)
            return TileType.PURPLELAND;

        // Gray mountain
        if (Mathf.Abs(c.r - c.g) < 0.08f &&
            Mathf.Abs(c.g - c.b) < 0.08f &&
            c.grayscale > 0.35f)
        {
            return TileType.MOUNTAIN;
        }

        return TileType.GRASSLAND;
    }

    private static void DebugSampleCounts(TileType[,] sample)
    {
        int grass = 0, water = 0, purple = 0, mountain = 0;

        int w = sample.GetLength(0);
        int h = sample.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                switch (sample[x, y])
                {
                    case TileType.GRASSLAND: grass++; break;
                    case TileType.OCEAN_DEEP: water++; break;
                    case TileType.PURPLELAND: purple++; break;
                    case TileType.MOUNTAIN: mountain++; break;
                }
            }
        }

        Debug.Log($"Sample counts -> Grass: {grass}, Water: {water}, Purple: {purple}, Mountain: {mountain}");
    }
}