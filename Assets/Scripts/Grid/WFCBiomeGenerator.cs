using System;
using System.Collections.Generic;
using UnityEngine;

public static class WFCBiomeGenerator
{
    private const int MaxAttempts = 12;
    private const float SampleWeightStrength = 2.2f;
    private const float RatioWeightStrength = 1.35f;
    private const float BorderWaterBias = 3.0f;
    private const float InteriorWaterPenalty = 0.35f;

    private static readonly TileType[] AllBiomeTypes =
    {
        TileType.GRASSLAND,
        TileType.PURPLELAND,
        TileType.MOUNTAIN_1,
        TileType.MOUNTAIN_2,
        TileType.MOUNTAIN_3,
        TileType.OCEAN_DEEP    
    };

    private static readonly HashSet<TileType> PlayableBiomeTypes = new HashSet<TileType>(AllBiomeTypes);

    private class SampleStats
    {
        public readonly Dictionary<TileType, int> TileCounts = new Dictionary<TileType, int>();
        public readonly Dictionary<TileType, float> TileRatios = new Dictionary<TileType, float>();
        public readonly Dictionary<TileType, HashSet<TileType>> AllowedNeighbors = new Dictionary<TileType, HashSet<TileType>>();
        public readonly Dictionary<TileType, Dictionary<TileType, int>> NeighborCounts = new Dictionary<TileType, Dictionary<TileType, int>>();
        public readonly Dictionary<Vector2Int, TileType> Anchors = new Dictionary<Vector2Int, TileType>();
        public int SampleWidth;
        public int SampleHeight;
        public int TotalCells;
    }

    public static void Generate(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        if (gridManager.biomeControlMask == null)
        {
            Debug.LogWarning("WFCBiomeGenerator: No biomeControlMask assigned. Falling back to StaticBiomeGenerator.");
            StaticBiomeGenerator.Generate(gridManager, plannedTypes);
            return;
        }

        SampleStats stats = AnalyzeSample(gridManager.biomeControlMask);
        if (stats.TotalCells <= 0)
        {
            Debug.LogWarning("WFCBiomeGenerator: Sample analysis failed. Falling back to StaticBiomeGenerator.");
            StaticBiomeGenerator.Generate(gridManager, plannedTypes);
            return;
        }

        DebugStats(stats);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (TryGenerate(gridManager, plannedTypes, stats, attempt))
            {
                Debug.Log($"WFCBiomeGenerator succeeded on attempt {attempt + 1}.");
                return;
            }
        }

        Debug.LogWarning("WFCBiomeGenerator failed after all attempts. Falling back to StaticBiomeGenerator.");
        StaticBiomeGenerator.Generate(gridManager, plannedTypes);
    }

    private static SampleStats AnalyzeSample(Texture2D texture)
    {
        SampleStats stats = new SampleStats
        {
            SampleWidth = texture.width,
            SampleHeight = texture.height,
            TotalCells = texture.width * texture.height
        };

        foreach (TileType type in AllBiomeTypes)
        {
            stats.TileCounts[type] = 0;
            stats.TileRatios[type] = 0f;
            stats.AllowedNeighbors[type] = new HashSet<TileType> { type };
            stats.NeighborCounts[type] = new Dictionary<TileType, int>();
        }

        TileType[,] sample = new TileType[texture.width, texture.height];

        for (int q = 0; q < texture.width; q++)
        {
            for (int r = 0; r < texture.height; r++)
            {
                TileType type = ClassifySampleColour(texture.GetPixel(q, r));
                sample[q, r] = type;
                stats.TileCounts[type]++;
            }
        }

        Debug.Log(
            $"AnalyzeSample counts -> " +
            $"Grass={stats.TileCounts[TileType.GRASSLAND]}, " +
            $"Purple={stats.TileCounts[TileType.PURPLELAND]}, " +
            $"Mountain1={stats.TileCounts[TileType.MOUNTAIN_1]}, " +
            $"Mountain2={stats.TileCounts[TileType.MOUNTAIN_2]}, " +
            $"Mountain3={stats.TileCounts[TileType.MOUNTAIN_3]}, " +
            $"Ocean={stats.TileCounts[TileType.OCEAN_DEEP]}"
        );

        foreach (TileType type in AllBiomeTypes)
        {
            stats.TileRatios[type] = stats.TotalCells > 0 ? (float)stats.TileCounts[type] / stats.TotalCells : 0f;
        }

        for (int q = 0; q < texture.width; q++)
        {
            for (int r = 0; r < texture.height; r++)
            {
                TileType current = sample[q, r];
                foreach (Vector2Int dir in HexMath.GetNeighborDirections(q))
                {
                    int nq = q + dir.x;
                    int nr = r + dir.y;
                    if (nq < 0 || nq >= texture.width || nr < 0 || nr >= texture.height)
                        continue;

                    TileType neighbor = sample[nq, nr];
                    stats.AllowedNeighbors[current].Add(neighbor);

                    if (!stats.NeighborCounts[current].ContainsKey(neighbor))
                        stats.NeighborCounts[current][neighbor] = 0;

                    stats.NeighborCounts[current][neighbor]++;
                }
            }
        }

        BuildAnchors(sample, stats);
        EnsureSafeFallbackRules(stats);
        return stats;
    }

    private static void BuildAnchors(TileType[,] sample, SampleStats stats)
    {
        int width = sample.GetLength(0);
        int height = sample.GetLength(1);

        Vector2Int[] normalizedAnchorPositions =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, 2),
            new Vector2Int(2, 1),
            new Vector2Int(2, 2),
            new Vector2Int(3, 1),
            new Vector2Int(1, 3),
            new Vector2Int(3, 3)
        };

        foreach (Vector2Int normalized in normalizedAnchorPositions)
        {
            int q = Mathf.Clamp(Mathf.RoundToInt((normalized.x / 4f) * (width - 1)), 0, width - 1);
            int r = Mathf.Clamp(Mathf.RoundToInt((normalized.y / 4f) * (height - 1)), 0, height - 1);
            stats.Anchors[new Vector2Int(normalized.x, normalized.y)] = sample[q, r];
        }
    }

    private static void EnsureSafeFallbackRules(SampleStats stats)
    {
        stats.AllowedNeighbors[TileType.GRASSLAND].Add(TileType.PURPLELAND);
        stats.AllowedNeighbors[TileType.PURPLELAND].Add(TileType.GRASSLAND);
        stats.AllowedNeighbors[TileType.PURPLELAND].Add(TileType.PURPLELAND);
        stats.AllowedNeighbors[TileType.OCEAN_DEEP].Add(TileType.OCEAN_DEEP);

        // Mountain adjacency rules: mostly allow altitude difference <= 1
        stats.AllowedNeighbors[TileType.MOUNTAIN_1].Add(TileType.MOUNTAIN_1);
        stats.AllowedNeighbors[TileType.MOUNTAIN_1].Add(TileType.MOUNTAIN_2);
        stats.AllowedNeighbors[TileType.MOUNTAIN_1].Add(TileType.GRASSLAND);

        stats.AllowedNeighbors[TileType.MOUNTAIN_2].Add(TileType.MOUNTAIN_1);
        stats.AllowedNeighbors[TileType.MOUNTAIN_2].Add(TileType.MOUNTAIN_2);
        stats.AllowedNeighbors[TileType.MOUNTAIN_2].Add(TileType.MOUNTAIN_3);

        stats.AllowedNeighbors[TileType.MOUNTAIN_3].Add(TileType.MOUNTAIN_2);
        stats.AllowedNeighbors[TileType.MOUNTAIN_3].Add(TileType.MOUNTAIN_3);

        stats.AllowedNeighbors[TileType.GRASSLAND].Add(TileType.MOUNTAIN_1);
    }

    private static bool TryGenerate(HexGridManager gridManager, TileType[,] plannedTypes, SampleStats stats, int attempt)
    {
        int totalWidth = gridManager.TotalWidth;
        int totalHeight = gridManager.TotalHeight;
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        for (int q = 0; q < totalWidth; q++)
        {
            for (int r = 0; r < totalHeight; r++)
            {
                plannedTypes[q, r] = TileType.OCEAN_DEEP;
            }
        }

        HashSet<TileType>[,] wave = new HashSet<TileType>[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                wave[q, r] = BuildInitialDomain(q, r, width, height);
                if (wave[q, r].Count == 0)
                    return false;
            }
        }

        ApplyAnchors(wave, queue, stats, width, height);

        if (!Propagate(wave, queue, width, height, stats))
            return false;

        while (true)
        {
            Vector2Int cell = FindLowestEntropyCell(wave, width, height, stats);
            if (cell.x < 0)
                break;

            TileType chosen = ChooseWeightedTile(wave[cell.x, cell.y], cell.x, cell.y, width, height, stats, wave, attempt);
            wave[cell.x, cell.y].Clear();
            wave[cell.x, cell.y].Add(chosen);
            queue.Enqueue(cell);

            if (!Propagate(wave, queue, width, height, stats))
                return false;
        }

        if (!WriteResult(gridManager, plannedTypes, wave, stats))
            return false;

        EnforceTargetRatios(gridManager, plannedTypes, stats);
        ForceMountainPresence(gridManager, plannedTypes);

        DebugFinalCounts(gridManager, plannedTypes);

        return true;
    }

    private static HashSet<TileType> BuildInitialDomain(int q, int r, int width, int height)
    {
        bool nearInnerBorder = q <= 1 || r <= 1 || q >= width - 2 || r >= height - 2;
        if (nearInnerBorder)
            return new HashSet<TileType> { TileType.GRASSLAND, TileType.PURPLELAND, TileType.OCEAN_DEEP };

        return new HashSet<TileType>(PlayableBiomeTypes);
    }

    private static void ApplyAnchors(HashSet<TileType>[,] wave, Queue<Vector2Int> queue, SampleStats stats, int width, int height)
    {
        foreach (KeyValuePair<Vector2Int, TileType> pair in stats.Anchors)
        {
            int q = Mathf.Clamp(Mathf.RoundToInt((pair.Key.x / 4f) * (width - 1)), 0, width - 1);
            int r = Mathf.Clamp(Mathf.RoundToInt((pair.Key.y / 4f) * (height - 1)), 0, height - 1);

            wave[q, r].Clear();
            wave[q, r].Add(pair.Value);
            queue.Enqueue(new Vector2Int(q, r));
        }
    }

    private static bool Propagate(HashSet<TileType>[,] wave, Queue<Vector2Int> queue, int width, int height, SampleStats stats)
    {
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            HashSet<TileType> currentDomain = wave[current.x, current.y];

            foreach (Vector2Int dir in HexMath.GetNeighborDirections(current.x))
            {
                int nq = current.x + dir.x;
                int nr = current.y + dir.y;
                if (nq < 0 || nq >= width || nr < 0 || nr >= height)
                    continue;

                HashSet<TileType> neighborDomain = wave[nq, nr];
                HashSet<TileType> reduced = new HashSet<TileType>();

                foreach (TileType neighborType in neighborDomain)
                {
                    bool supported = false;
                    foreach (TileType currentType in currentDomain)
                    {
                        if (stats.AllowedNeighbors[currentType].Contains(neighborType) &&
                            stats.AllowedNeighbors[neighborType].Contains(currentType))
                        {
                            supported = true;
                            break;
                        }
                    }

                    if (supported)
                        reduced.Add(neighborType);
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

    private static Vector2Int FindLowestEntropyCell(HashSet<TileType>[,] wave, int width, int height, SampleStats stats)
    {
        float bestEntropy = float.PositiveInfinity;
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                HashSet<TileType> domain = wave[q, r];
                if (domain.Count <= 1)
                    continue;

                float entropy = ComputeEntropy(domain, stats);
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

    private static float ComputeEntropy(HashSet<TileType> domain, SampleStats stats)
    {
        float totalWeight = 0f;
        float logSum = 0f;

        foreach (TileType type in domain)
        {
            float w = Mathf.Max(0.0001f, stats.TileRatios[type]);
            totalWeight += w;
            logSum += w * Mathf.Log(w);
        }

        return Mathf.Log(totalWeight) - (logSum / totalWeight) + UnityEngine.Random.value * 0.0001f;
    }

    private static TileType ChooseWeightedTile(
        HashSet<TileType> domain,
        int q,
        int r,
        int width,
        int height,
        SampleStats stats,
        HashSet<TileType>[,] wave,
        int attempt)
    {
        List<TileType> types = new List<TileType>(domain);
        List<float> weights = new List<float>(types.Count);
        float total = 0f;

        foreach (TileType type in types)
        {
            float weight = ComputeSelectionWeight(type, q, r, width, height, stats, wave, attempt);
            weights.Add(weight);
            total += weight;
        }

        float roll = UnityEngine.Random.value * total;
        float running = 0f;
        for (int i = 0; i < types.Count; i++)
        {
            running += weights[i];
            if (roll <= running)
                return types[i];
        }

        return types[types.Count - 1];
    }

    private static float ComputeSelectionWeight(
        TileType type,
        int q,
        int r,
        int width,
        int height,
        SampleStats stats,
        HashSet<TileType>[,] wave,
        int attempt)
    {
        float ratio = Mathf.Max(0.0001f, stats.TileRatios[type]);
        float weight = Mathf.Pow(ratio, RatioWeightStrength);

        float u = width > 1 ? q / (float)(width - 1) : 0f;
        float v = height > 1 ? r / (float)(height - 1) : 0f;
        float sampleU = Mathf.Clamp01((u + attempt * 0.071f) % 1f);
        float sampleV = Mathf.Clamp01((v + attempt * 0.113f) % 1f);
        int sampleQ = Mathf.Clamp(Mathf.RoundToInt(sampleU * (stats.SampleWidth - 1)), 0, stats.SampleWidth - 1);
        int sampleR = Mathf.Clamp(Mathf.RoundToInt(sampleV * (stats.SampleHeight - 1)), 0, stats.SampleHeight - 1);

        TileType sampledType = SampleAt(stats, sampleQ, sampleR);
        if (sampledType == type)
            weight *= SampleWeightStrength;

        bool nearBorder = q <= 1 || r <= 1 || q >= width - 2 || r >= height - 2;
        if (nearBorder)
        {
            if (type == TileType.OCEAN_DEEP)
                weight *= BorderWaterBias;

            if (type == TileType.MOUNTAIN_1 ||
                type == TileType.MOUNTAIN_2 ||
                type == TileType.MOUNTAIN_3)
            {
                weight *= 0.2f;
            }
        }
        else if (type == TileType.OCEAN_DEEP)
        {
            weight *= InteriorWaterPenalty;
        }

        int collapsedNeighbors = 0;
        int matchingNeighbors = 0;
        int mountainNeighbors = 0;
        int sameTypeNeighbors = 0;

        foreach (Vector2Int dir in HexMath.GetNeighborDirections(q))
        {
            int nq = q + dir.x;
            int nr = r + dir.y;
            if (nq < 0 || nq >= width || nr < 0 || nr >= height)
                continue;

            HashSet<TileType> domain = wave[nq, nr];
            if (domain.Count == 1)
            {
                collapsedNeighbors++;
                TileType neighbor = First(domain);

                if (stats.NeighborCounts[type].TryGetValue(neighbor, out int count))
                    matchingNeighbors += count;
                else if (stats.AllowedNeighbors[type].Contains(neighbor))
                    matchingNeighbors += 1;

                if (HexGridManager.IsMountainType(neighbor))
                    mountainNeighbors++;

                if (neighbor == type)
                    sameTypeNeighbors++;
            }
        }

        if (collapsedNeighbors > 0)
            weight *= 1f + (matchingNeighbors / (float)(collapsedNeighbors + 1));

        // Preserve rare mountain tiers a little more strongly
        if (type == TileType.MOUNTAIN_3)
        {
            weight *= 1.75f;
            if (mountainNeighbors == 0)
                weight *= 0.55f; // avoid isolated peaks
            if (mountainNeighbors >= 1)
                weight *= 1.2f;
        }
        else if (type == TileType.MOUNTAIN_2)
        {
            weight *= 1.25f;
            if (sameTypeNeighbors >= 2)
                weight *= 1.1f;
        }
        else if (type == TileType.MOUNTAIN_1)
        {
            weight *= 1.05f;
        }

        return Mathf.Max(0.0001f, weight);
    }

    private static TileType SampleAt(SampleStats stats, int q, int r)
    {
        float u = stats.SampleWidth > 1 ? q / (float)(stats.SampleWidth - 1) : 0f;
        float v = stats.SampleHeight > 1 ? r / (float)(stats.SampleHeight - 1) : 0f;

        if (stats.Anchors.TryGetValue(new Vector2Int(Mathf.RoundToInt(u * 4f), Mathf.RoundToInt(v * 4f)), out TileType anchorType))
            return anchorType;

       if (v < 0.12f)
            return TileType.MOUNTAIN_3;
        if (v < 0.20f)
            return TileType.MOUNTAIN_2;
        if (v < 0.28f)
            return TileType.MOUNTAIN_1;

        if (u > 0.70f && v > 0.55f)
            return TileType.PURPLELAND;
        if (u < 0.18f || v < 0.12f || u > 0.88f || v > 0.88f)
            return TileType.OCEAN_DEEP;

        return TileType.GRASSLAND;
    }

    private static bool WriteResult(HexGridManager gridManager, TileType[,] plannedTypes, HashSet<TileType>[,] wave, SampleStats stats)
    {
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                if (wave[q, r].Count == 0)
                    return false;

                plannedTypes[playableOffsetQ + q, playableOffsetR + r] = First(wave[q, r]);
            }
        }

        return true;
    }

    private static void EnforceTargetRatios(HexGridManager gridManager, TileType[,] plannedTypes, SampleStats stats)
    {
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;
        int totalPlayable = width * height;

        Dictionary<TileType, int> counts = new Dictionary<TileType, int>();

        TileType[] enforcementOrder =
        {
            TileType.OCEAN_DEEP,
            TileType.PURPLELAND,
            TileType.MOUNTAIN_2,
            TileType.MOUNTAIN_1,
            TileType.MOUNTAIN_3,
            TileType.GRASSLAND
        };

        foreach (TileType type in AllBiomeTypes)
            counts[type] = 0;

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                counts[plannedTypes[playableOffsetQ + q, playableOffsetR + r]]++;
            }
        }

        foreach (TileType targetType in enforcementOrder)
        {
            int targetCount = Mathf.RoundToInt(stats.TileRatios[targetType] * totalPlayable);
            int safety = totalPlayable;

            while (counts[targetType] < targetCount && safety-- > 0)
            {
                Vector2Int candidate = FindConvertibleCell(gridManager, plannedTypes, counts, targetType, stats, true);
                if (candidate.x < 0)
                    break;

                TileType oldType = plannedTypes[candidate.x, candidate.y];
                plannedTypes[candidate.x, candidate.y] = targetType;
                counts[oldType]--;
                counts[targetType]++;
            }
        }
    }

    private static Vector2Int FindConvertibleCell(
        HexGridManager gridManager,
        TileType[,] plannedTypes,
        Dictionary<TileType, int> counts,
        TileType targetType,
        SampleStats stats,
        bool requireCompatibility)
    {
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                int worldQ = playableOffsetQ + q;
                int worldR = playableOffsetR + r;
                TileType current = plannedTypes[worldQ, worldR];
                if (current == targetType)
                    continue;

                if (HexGridManager.IsMountainType(current) && !HexGridManager.IsMountainType(targetType))
                    continue;

                bool nearBorder = q <= 1 || r <= 1 || q >= width - 2 || r >= height - 2;
                if (nearBorder && HexGridManager.IsMountainType(targetType))
                    continue;

                if (requireCompatibility && !CanPlaceTypeAt(q, r, targetType, plannedTypes, gridManager, stats))
                    continue;

                candidates.Add(new Vector2Int(worldQ, worldR));
            }
        }

        if (candidates.Count == 0)
            return new Vector2Int(-1, -1);

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static bool CanPlaceTypeAt(int localQ, int localR, TileType targetType, TileType[,] plannedTypes, HexGridManager gridManager, SampleStats stats)
    {
        int worldQ = gridManager.PlayableOffsetQ + localQ;
        int worldR = gridManager.PlayableOffsetR + localR;

        foreach (Vector2Int dir in HexMath.GetNeighborDirections(localQ))
        {
            int nq = localQ + dir.x;
            int nr = localR + dir.y;
            if (nq < 0 || nq >= gridManager.width || nr < 0 || nr >= gridManager.height)
                continue;

            TileType neighbor = plannedTypes[gridManager.PlayableOffsetQ + nq, gridManager.PlayableOffsetR + nr];
            if (!stats.AllowedNeighbors[targetType].Contains(neighbor) || !stats.AllowedNeighbors[neighbor].Contains(targetType))
                return false;
        }

        return true;
    }

    private static void ForceMountainPresence(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        bool foundMountain1 = false;
        bool foundMountain2 = false;
        bool foundMountain3 = false;

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                TileType type = plannedTypes[playableOffsetQ + q, playableOffsetR + r];
                if (type == TileType.MOUNTAIN_1) foundMountain1 = true;
                if (type == TileType.MOUNTAIN_2) foundMountain2 = true;
                if (type == TileType.MOUNTAIN_3) foundMountain3 = true;
            }
        }

        int centerQ = playableOffsetQ + width / 2;
        int centerR = playableOffsetR + height / 2;

        if (!foundMountain2)
            plannedTypes[centerQ, centerR] = TileType.MOUNTAIN_2;

        foreach (Vector2Int dir in HexMath.GetNeighborDirections(centerQ))
        {
            int nq = centerQ + dir.x;
            int nr = centerR + dir.y;

            if (nq < playableOffsetQ || nq >= playableOffsetQ + width ||
                nr < playableOffsetR || nr >= playableOffsetR + height)
                continue;

            if (!foundMountain1 && plannedTypes[nq, nr] == TileType.GRASSLAND)
            {
                plannedTypes[nq, nr] = TileType.MOUNTAIN_1;
                foundMountain1 = true;
            }
            else if (!foundMountain3 && plannedTypes[nq, nr] == TileType.GRASSLAND)
            {
                plannedTypes[nq, nr] = TileType.MOUNTAIN_3;
                foundMountain3 = true;
            }
        }
    }

    private static TileType First(HashSet<TileType> set)
    {
        foreach (TileType type in set)
            return type;
        return TileType.GRASSLAND;
    }

    private static TileType ClassifySampleColour(Color c)
    {
        Color32 color = (Color32)c;

        // temporary debug for mountain-like greys
        if (Mathf.Abs(color.r - color.g) <= 3 && Mathf.Abs(color.g - color.b) <= 3)
        {
            if (color.r >= 85 && color.r <= 205)
            {
                Debug.Log($"Sample grey pixel seen: ({color.r}, {color.g}, {color.b})");
            }
        }


        if (IsNear(color, 50, 220, 50, 8))
            return TileType.GRASSLAND;

        if (IsNear(color, 40, 120, 230, 8))
            return TileType.OCEAN_DEEP;

        if (IsNear(color, 100, 100, 100, 20))
            return TileType.MOUNTAIN_1;

        if (IsNear(color, 140, 140, 140, 20))
            return TileType.MOUNTAIN_2;

        if (IsNear(color, 255, 255, 255, 20))
            return TileType.MOUNTAIN_3;

        if (IsNear(color, 180, 60, 220, 8))
            return TileType.PURPLELAND;

        Debug.LogWarning($"Unknown control-mask color: ({color.r}, {color.g}, {color.b}). Defaulting to GRASSLAND.");
        return TileType.GRASSLAND;
    }

    private static bool IsNear(Color32 color, int r, int g, int b, int tolerance)
    {
        return Mathf.Abs(color.r - r) <= tolerance &&
            Mathf.Abs(color.g - g) <= tolerance &&
            Mathf.Abs(color.b - b) <= tolerance;
    }


    private static void DebugStats(SampleStats stats)
    {
        Debug.Log(
            $"Sample ratios => Grass: {stats.TileRatios[TileType.GRASSLAND]:P1}, " +
            $"Purple: {stats.TileRatios[TileType.PURPLELAND]:P1}, " +
            $"Mountain1: {stats.TileRatios[TileType.MOUNTAIN_1]:P1}, " +
            $"Mountain2: {stats.TileRatios[TileType.MOUNTAIN_2]:P1}, " +
            $"Mountain3: {stats.TileRatios[TileType.MOUNTAIN_3]:P1}, " +
            $"Ocean: {stats.TileRatios[TileType.OCEAN_DEEP]:P1}");

        foreach (TileType type in AllBiomeTypes)
        {
            string line = type + " allows: ";
            bool first = true;
            foreach (TileType neighbor in stats.AllowedNeighbors[type])
            {
                if (!first) line += ", ";
                line += neighbor;
                first = false;
            }
            Debug.Log(line);
        }
    }

    private static void DebugFinalCounts(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        int grass = 0;
        int purple = 0;
        int mountain1 = 0;
        int mountain2 = 0;
        int mountain3 = 0;
        int ocean = 0;

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                TileType type = plannedTypes[playableOffsetQ + q, playableOffsetR + r];
                switch (type)
                {
                    case TileType.GRASSLAND: grass++; break;
                    case TileType.PURPLELAND: purple++; break;
                    case TileType.MOUNTAIN_1: mountain1++; break;
                    case TileType.MOUNTAIN_2: mountain2++; break;
                    case TileType.MOUNTAIN_3: mountain3++; break;
                    case TileType.OCEAN_DEEP: ocean++; break;
                }
            }
        }

        Debug.Log(
            $"WFC final counts -> " +
            $"Grass={grass}, Purple={purple}, " +
            $"Mountain1={mountain1}, Mountain2={mountain2}, Mountain3={mountain3}, " +
            $"Ocean={ocean}"
        );
    }
}
