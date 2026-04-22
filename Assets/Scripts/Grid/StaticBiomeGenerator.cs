using System.Collections.Generic;
using UnityEngine;

public static class StaticBiomeGenerator
{
    public static void Generate(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        float purpleNoiseOffsetX = Random.Range(0f, 999f);
        float purpleNoiseOffsetY = Random.Range(0f, 999f);
        float oceanNoiseOffsetX = Random.Range(0f, 999f);
        float oceanNoiseOffsetY = Random.Range(0f, 999f);

        float biomeScale = 0.18f;
        float oceanScale = 0.14f;

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

        // 1) Fill the base map first: ocean / grass / purple
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

                if (isNearInnerBorder)
                {
                    plannedTypes[q, r] = purpleNoise < gridManager.purpleChance
                        ? TileType.PURPLELAND
                        : TileType.GRASSLAND;
                    continue;
                }

                if (oceanNoise < gridManager.oceanChance)
                {
                    plannedTypes[q, r] = TileType.OCEAN_DEEP;
                    continue;
                }

                plannedTypes[q, r] = purpleNoise < gridManager.purpleChance
                    ? TileType.PURPLELAND
                    : TileType.GRASSLAND;
            }
        }

        // 2) Stamp mountain ranges with altitude-aware growth
        int clusterCount = Mathf.Max(3, Mathf.RoundToInt((width * height) / 140f));
        int minClusterSize = 7;
        int maxClusterSize = 18;

        for (int i = 0; i < clusterCount; i++)
        {
            int seedQ = Random.Range(playableOffsetQ + 2, playableOffsetQ + width - 2);
            int seedR = Random.Range(playableOffsetR + 2, playableOffsetR + height - 2);

            if (plannedTypes[seedQ, seedR] == TileType.OCEAN_DEEP)
                continue;

            int targetSize = Random.Range(minClusterSize, maxClusterSize + 1);

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            Dictionary<Vector2Int, int> heights = new Dictionary<Vector2Int, int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            Vector2Int seed = new Vector2Int(seedQ, seedR);
            frontier.Enqueue(seed);
            visited.Add(seed);

            // Start each range near the middle/high end so it can slope outward
            heights[seed] = Random.Range(2, 4); // 2 or 3

            int placed = 0;

            while (frontier.Count > 0 && placed < targetSize)
            {
                Vector2Int current = frontier.Dequeue();
                int cq = current.x;
                int cr = current.y;

                int localQ = cq - playableOffsetQ;
                int localR = cr - playableOffsetR;

                bool isNearInnerBorder =
                    localQ <= 1 ||
                    localR <= 1 ||
                    localQ >= width - 2 ||
                    localR >= height - 2;

                if (!isNearInnerBorder && plannedTypes[cq, cr] != TileType.OCEAN_DEEP)
                {
                    int altitude = Mathf.Clamp(heights[current], 1, 3);
                    plannedTypes[cq, cr] = MountainTypeFromAltitude(altitude);
                    placed++;
                }

                List<Vector2Int> neighbors = new List<Vector2Int>(HexMath.GetNeighborDirections(cq));
                Shuffle(neighbors);

                foreach (Vector2Int dir in neighbors)
                {
                    int nq = cq + dir.x;
                    int nr = cr + dir.y;

                    if (nq < playableOffsetQ + 1 || nq >= playableOffsetQ + width - 1 ||
                        nr < playableOffsetR + 1 || nr >= playableOffsetR + height - 1)
                        continue;

                    Vector2Int next = new Vector2Int(nq, nr);
                    if (visited.Contains(next))
                        continue;

                    visited.Add(next);

                    if (plannedTypes[nq, nr] == TileType.OCEAN_DEEP)
                        continue;

                    // Grow clusters with fairly high continuity
                    if (Random.value >= 0.80f)
                        continue;

                    int currentHeight = heights[current];
                    int nextHeight;

                    // Mostly keep same height or differ by 1, rare cliff by 2
                    float roll = Random.value;
                    if (roll < 0.40f)
                    {
                        nextHeight = currentHeight;
                    }
                    else if (roll < 0.75f)
                    {
                        nextHeight = currentHeight - 1;
                    }
                    else if (roll < 0.95f)
                    {
                        nextHeight = currentHeight + 1;
                    }
                    else
                    {
                        nextHeight = currentHeight + (Random.value < 0.5f ? -2 : 2);
                    }

                    nextHeight = Mathf.Clamp(nextHeight, 1, 3);
                    heights[next] = nextHeight;
                    frontier.Enqueue(next);
                }
            }
        }

        // 3) Hard guarantee: if somehow none were placed, force one center range
        bool foundMountain = false;

        for (int q = playableOffsetQ; q < playableOffsetQ + width && !foundMountain; q++)
        {
            for (int r = playableOffsetR; r < playableOffsetR + height; r++)
            {
                TileType type = plannedTypes[q, r];
                if (type == TileType.MOUNTAIN_1 ||
                    type == TileType.MOUNTAIN_2 ||
                    type == TileType.MOUNTAIN_3)
                {
                    foundMountain = true;
                    break;
                }
            }
        }

        if (!foundMountain)
        {
            int centerQ = playableOffsetQ + width / 2;
            int centerR = playableOffsetR + height / 2;

            plannedTypes[centerQ, centerR] = TileType.MOUNTAIN_2;

            foreach (Vector2Int dir in HexMath.GetNeighborDirections(centerQ))
            {
                int nq = centerQ + dir.x;
                int nr = centerR + dir.y;

                if (nq >= playableOffsetQ + 1 && nq < playableOffsetQ + width - 1 &&
                    nr >= playableOffsetR + 1 && nr < playableOffsetR + height - 1 &&
                    plannedTypes[nq, nr] != TileType.OCEAN_DEEP)
                {
                    plannedTypes[nq, nr] = TileType.MOUNTAIN_1;
                }
            }
        }
    }

    private static void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static TileType MountainTypeFromAltitude(int altitude)
    {
        switch (Mathf.Clamp(altitude, 1, 3))
        {
            case 1: return TileType.MOUNTAIN_1;
            case 2: return TileType.MOUNTAIN_2;
            case 3: return TileType.MOUNTAIN_3;
            default: return TileType.MOUNTAIN_1;
        }
    }

    private static void StampMountainRange(
    HexGridManager gridManager,
    TileType[,] plannedTypes,
    int seedQ,
    int seedR,
    int targetSize)
    {
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> heights = new Dictionary<Vector2Int, int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int seed = new Vector2Int(seedQ, seedR);
        frontier.Enqueue(seed);
        visited.Add(seed);
        heights[seed] = Random.Range(2, 4); // seed at 2 or 3

        int placed = 0;

        while (frontier.Count > 0 && placed < targetSize)
        {
            Vector2Int current = frontier.Dequeue();
            int cq = current.x;
            int cr = current.y;

            int localQ = cq - playableOffsetQ;
            int localR = cr - playableOffsetR;

            bool isNearInnerBorder =
                localQ <= 1 ||
                localR <= 1 ||
                localQ >= width - 2 ||
                localR >= height - 2;

            if (!isNearInnerBorder && plannedTypes[cq, cr] != TileType.OCEAN_DEEP)
            {
                int altitude = heights[current];
                plannedTypes[cq, cr] = MountainTypeFromAltitude(altitude);
                placed++;
            }

            List<Vector2Int> neighbors = new List<Vector2Int>(HexMath.GetNeighborDirections(cq));
            Shuffle(neighbors);

            foreach (Vector2Int dir in neighbors)
            {
                int nq = cq + dir.x;
                int nr = cr + dir.y;

                if (nq < playableOffsetQ + 1 || nq >= playableOffsetQ + width - 1 ||
                    nr < playableOffsetR + 1 || nr >= playableOffsetR + height - 1)
                    continue;

                Vector2Int next = new Vector2Int(nq, nr);
                if (visited.Contains(next))
                    continue;

                visited.Add(next);

                if (plannedTypes[nq, nr] == TileType.OCEAN_DEEP)
                    continue;

                if (Random.value > 0.80f)
                    continue;

                int currentHeight = heights[current];
                int deltaRoll = Random.Range(0, 100);

                int nextHeight;
                if (deltaRoll < 40) nextHeight = currentHeight;
                else if (deltaRoll < 75) nextHeight = currentHeight - 1;
                else if (deltaRoll < 95) nextHeight = currentHeight + 1;
                else nextHeight = currentHeight + (Random.value < 0.5f ? -2 : 2); // rare cliff

                nextHeight = Mathf.Clamp(nextHeight, 1, 3);

                heights[next] = nextHeight;
                frontier.Enqueue(next);
            }
        }
    }
}