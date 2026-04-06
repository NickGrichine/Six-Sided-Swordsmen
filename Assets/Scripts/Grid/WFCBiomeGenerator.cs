using System.Collections.Generic;
using UnityEngine;

public static class WFCBiomeGenerator
{
    private class WfcCell
    {
        public HashSet<TileType> possible = new HashSet<TileType>();
        public bool IsCollapsed => possible.Count == 1;
    }

    private const int MaxAttempts = 20;

    private enum ControlRegion
    {
        Neutral,
        Water,
        Grass,
        Purple,
        Mountain
    }

    public static void Generate(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (TryGenerate(gridManager, plannedTypes))
            {
                Debug.Log($"WFC succeeded on attempt {attempt + 1}");
                return;
            }
        }

        Debug.LogWarning("WFC failed after all attempts. Falling back to StaticBiomeGenerator.");
        StaticBiomeGenerator.Generate(gridManager, plannedTypes);
    }

    private static bool TryGenerate(HexGridManager gridManager, TileType[,] plannedTypes)
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

                Color controlColor = SampleControlColor(gridManager, localQ, localR);
                ControlRegion region = GetControlRegion(controlColor);

                if (isNearInnerBorder)
                {
                    AddOptions(cells[q, r], TileType.GRASSLAND, TileType.PURPLELAND);
                }
                else
                {
                    ApplyInitialDomainForRegion(cells[q, r], region);
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

        if (!Propagate(cells, queue, totalWidth, totalHeight))
            return false;

        while (true)
        {
            Vector2Int next = FindLowestEntropyCell(cells, totalWidth, totalHeight);

            if (next.x == -1)
                break;

            int localQ = next.x - playableOffsetQ;
            int localR = next.y - playableOffsetR;

            Color controlColor = SampleControlColor(gridManager, localQ, localR);
            TileType chosen = ChooseWeightedTile(cells[next.x, next.y].possible, controlColor);

            cells[next.x, next.y].possible.Clear();
            cells[next.x, next.y].possible.Add(chosen);

            queue.Enqueue(next);

            if (!Propagate(cells, queue, totalWidth, totalHeight))
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

        PostProcessTowardControlMap(gridManager, plannedTypes);

        return true;
    }

    private static void AddOptions(WfcCell cell, params TileType[] types)
    {
        foreach (TileType t in types)
            cell.possible.Add(t);
    }

    private static void ApplyInitialDomainForRegion(WfcCell cell, ControlRegion region)
    {
        switch (region)
        {
            case ControlRegion.Water:
                AddOptions(cell, TileType.OCEAN_DEEP, TileType.GRASSLAND);
                break;

            case ControlRegion.Grass:
                AddOptions(cell, TileType.GRASSLAND, TileType.PURPLELAND);
                break;

            case ControlRegion.Purple:
                AddOptions(cell, TileType.PURPLELAND, TileType.GRASSLAND);
                break;

            case ControlRegion.Mountain:
                AddOptions(cell, TileType.MOUNTAIN, TileType.GRASSLAND);
                break;

            case ControlRegion.Neutral:
            default:
                AddOptions(cell,
                    TileType.GRASSLAND,
                    TileType.PURPLELAND,
                    TileType.MOUNTAIN,
                    TileType.OCEAN_DEEP);
                break;
        }
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

    private static TileType ChooseWeightedTile(HashSet<TileType> options, Color controlColor)
    {
        int totalWeight = 0;

        foreach (TileType type in options)
            totalWeight += GetWeight(type, controlColor);

        if (totalWeight <= 0)
        {
            foreach (TileType type in options)
                return type;

            return TileType.GRASSLAND;
        }

        int roll = Random.Range(0, totalWeight);
        int running = 0;

        foreach (TileType type in options)
        {
            running += GetWeight(type, controlColor);
            if (roll < running)
                return type;
        }

        foreach (TileType type in options)
            return type;

        return TileType.GRASSLAND;
    }

    private static int GetWeight(TileType type, Color c)
    {
        ControlRegion region = GetControlRegion(c);

        switch (region)
        {
            case ControlRegion.Water:
                switch (type)
                {
                    case TileType.OCEAN_DEEP: return 220;
                    case TileType.GRASSLAND: return 20;
                    case TileType.PURPLELAND: return 5;
                    case TileType.MOUNTAIN: return 1;
                }
                break;

            case ControlRegion.Grass:
                switch (type)
                {
                    case TileType.GRASSLAND: return 220;
                    case TileType.PURPLELAND: return 25;
                    case TileType.MOUNTAIN: return 5;
                    case TileType.OCEAN_DEEP: return 10;
                }
                break;

            case ControlRegion.Purple:
                switch (type)
                {
                    case TileType.PURPLELAND: return 220;
                    case TileType.GRASSLAND: return 20;
                    case TileType.MOUNTAIN: return 5;
                    case TileType.OCEAN_DEEP: return 5;
                }
                break;

            case ControlRegion.Mountain:
                switch (type)
                {
                    case TileType.MOUNTAIN: return 220;
                    case TileType.GRASSLAND: return 20;
                    case TileType.PURPLELAND: return 10;
                    case TileType.OCEAN_DEEP: return 1;
                }
                break;

            case ControlRegion.Neutral:
            default:
                switch (type)
                {
                    case TileType.GRASSLAND: return 45;
                    case TileType.PURPLELAND: return 30;
                    case TileType.MOUNTAIN: return 15;
                    case TileType.OCEAN_DEEP: return 10;
                }
                break;
        }

        return 1;
    }

    private static bool Propagate(WfcCell[,] cells, Queue<Vector2Int> queue, int totalWidth, int totalHeight)
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
                        if (AreCompatible(currentOption, neighborOption))
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

    private static ControlRegion GetControlRegion(Color c)
    {
        if (c.b > 0.75f && c.r < 0.45f && c.g < 0.65f)
            return ControlRegion.Water;

        if (c.g > 0.75f && c.r < 0.55f && c.b < 0.55f)
            return ControlRegion.Grass;

        if (c.r > 0.75f && c.b > 0.75f && c.g < 0.55f)
            return ControlRegion.Purple;

        if (Mathf.Abs(c.r - c.g) < 0.08f &&
            Mathf.Abs(c.g - c.b) < 0.08f &&
            c.grayscale > 0.35f)
        {
            return ControlRegion.Mountain;
        }

        return ControlRegion.Neutral;
    }

    private static bool AreCompatible(TileType a, TileType b)
    {
        if (a == TileType.MOUNTAIN && b == TileType.OCEAN_DEEP) return false;
        if (a == TileType.OCEAN_DEEP && b == TileType.MOUNTAIN) return false;

        if (a == TileType.OCEAN_DEEP)
            return b == TileType.OCEAN_DEEP ||
                   b == TileType.GRASSLAND ||
                   b == TileType.PURPLELAND;

        if (a == TileType.MOUNTAIN)
            return b == TileType.MOUNTAIN ||
                   b == TileType.GRASSLAND ||
                   b == TileType.PURPLELAND;

        if (a == TileType.PURPLELAND)
            return b == TileType.PURPLELAND ||
                   b == TileType.GRASSLAND ||
                   b == TileType.MOUNTAIN ||
                   b == TileType.OCEAN_DEEP;

        if (a == TileType.GRASSLAND)
            return true;

        return false;
    }

    private static Color SampleControlColor(HexGridManager gridManager, int localQ, int localR)
    {
        if (gridManager.biomeControlMask == null)
            return Color.black;

        float u = (gridManager.width <= 1) ? 0f : (float)localQ / (gridManager.width - 1);
        float v = (gridManager.height <= 1) ? 0f : (float)localR / (gridManager.height - 1);

        return gridManager.biomeControlMask.GetPixelBilinear(u, v);
    }

    private static TileType GetPrimaryTypeForRegion(ControlRegion region)
    {
        switch (region)
        {
            case ControlRegion.Water: return TileType.OCEAN_DEEP;
            case ControlRegion.Grass: return TileType.GRASSLAND;
            case ControlRegion.Purple: return TileType.PURPLELAND;
            case ControlRegion.Mountain: return TileType.MOUNTAIN;
            default: return TileType.GRASSLAND;
        }
    }

    private static void PostProcessTowardControlMap(HexGridManager gridManager, TileType[,] plannedTypes)
    {
        int totalWidth = gridManager.TotalWidth;
        int totalHeight = gridManager.TotalHeight;
        int playableOffsetQ = gridManager.PlayableOffsetQ;
        int playableOffsetR = gridManager.PlayableOffsetR;
        int width = gridManager.width;
        int height = gridManager.height;

        for (int q = playableOffsetQ; q < playableOffsetQ + width; q++)
        {
            for (int r = playableOffsetR; r < playableOffsetR + height; r++)
            {
                int localQ = q - playableOffsetQ;
                int localR = r - playableOffsetR;

                Color c = SampleControlColor(gridManager, localQ, localR);
                ControlRegion region = GetControlRegion(c);
                TileType preferred = GetPrimaryTypeForRegion(region);
                TileType current = plannedTypes[q, r];

                if (current == preferred)
                    continue;

                if (CanReplaceWith(plannedTypes, q, r, preferred, totalWidth, totalHeight))
                    plannedTypes[q, r] = preferred;
            }
        }
    }

    private static bool CanReplaceWith(TileType[,] plannedTypes, int q, int r, TileType candidate, int totalWidth, int totalHeight)
    {
        foreach (Vector2Int dir in HexMath.GetNeighborDirections(q))
        {
            int nq = q + dir.x;
            int nr = r + dir.y;

            if (nq < 0 || nq >= totalWidth || nr < 0 || nr >= totalHeight)
                continue;

            TileType neighbor = plannedTypes[nq, nr];

            if (!AreCompatible(candidate, neighbor) || !AreCompatible(neighbor, candidate))
                return false;
        }

        return true;
    }
}