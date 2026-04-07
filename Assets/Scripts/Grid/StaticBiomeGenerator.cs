using UnityEngine;

public static class StaticBiomeGenerator 
{
    public static void Generate(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        float purpleNoiseOffsetX = Random.Range(0f, 999f);
        float purpleNoiseOffsetY = Random.Range(0f, 999f);
        float oceanNoiseOffsetX = Random.Range(0f, 999f);
        float oceanNoiseOffsetY = Random.Range(0f, 999f);
        float mountainNoiseOffsetX = Random.Range(0f, 999f);
        float mountainNoiseOffsetY = Random.Range(0f, 999f);

        float biomeScale = 0.18f;
        float oceanScale = 0.14f;
        float mountainScaleLocal = gridManager.mountainScale;

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

                if (mountainNoise > gridManager.mountainChance)
                {
                    plannedTypes[q, r] = TileType.MOUNTAIN;
                    continue;
                }

                if (mountainNoise > gridManager.mountainChance - 0.03f &&
                    Random.value < gridManager.mountainBlendChance)
                {
                    plannedTypes[q, r] = TileType.MOUNTAIN;
                    continue;
                }

                plannedTypes[q, r] = purpleNoise < gridManager.purpleChance
                    ? TileType.PURPLELAND
                    : TileType.GRASSLAND;
            }
        }
    }
}