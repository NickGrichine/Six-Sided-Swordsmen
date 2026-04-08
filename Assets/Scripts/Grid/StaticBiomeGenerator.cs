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

        // 2) Stamp guaranteed mountain clusters
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
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            Vector2Int seed = new Vector2Int(seedQ, seedR);
            frontier.Enqueue(seed);
            visited.Add(seed);

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
                    plannedTypes[cq, cr] = TileType.MOUNTAIN;
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

                    if (plannedTypes[nq, nr] != TileType.OCEAN_DEEP && Random.value < 0.80f)
                        frontier.Enqueue(next);
                }
            }
        }

        // 3) Hard guarantee: if somehow none were placed, force one center cluster
        bool foundMountain = false;

        for (int q = playableOffsetQ; q < playableOffsetQ + width && !foundMountain; q++)
        {
            for (int r = playableOffsetR; r < playableOffsetR + height; r++)
            {
                if (plannedTypes[q, r] == TileType.MOUNTAIN)
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

            plannedTypes[centerQ, centerR] = TileType.MOUNTAIN;

            foreach (Vector2Int dir in HexMath.GetNeighborDirections(centerQ))
            {
                int nq = centerQ + dir.x;
                int nr = centerR + dir.y;

                if (nq >= playableOffsetQ + 1 && nq < playableOffsetQ + width - 1 &&
                    nr >= playableOffsetR + 1 && nr < playableOffsetR + height - 1 &&
                    plannedTypes[nq, nr] != TileType.OCEAN_DEEP)
                {
                    plannedTypes[nq, nr] = TileType.MOUNTAIN;
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
}