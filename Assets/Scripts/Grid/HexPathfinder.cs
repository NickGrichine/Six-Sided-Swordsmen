using UnityEngine;
using System.Collections.Generic;

public static class HexPathfinder
{
    public static List<Tile> FindPath(Tile start, Tile goal)
    {

        //Basic edge case checks. Return empty pathlist.
        if (start == null || goal == null || start == goal) return new List<Tile>();
        if (!goal.passable || goal.IsOccupied)
        {
            Debug.LogWarning($"Pathfinding failed: goal tile '{goal.name}' is not passable or is occupied.");
            return new List<Tile>();
        }

        
        var openSet = new List<Tile> { start };
        var openSetLookup = new HashSet<Tile> { start };
        var closedSet = new HashSet<Tile>();

        var cameFrom = new Dictionary<Tile, Tile>();
        var gScore = new Dictionary<Tile, int> { [start] = 0 };
        var fScore = new Dictionary<Tile, int> { [start] = Heuristic(start, goal) };

         while (openSet.Count > 0)
        {
            Tile current = GetLowestFScore(openSet, fScore, goal);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, goal);
            }

            openSet.Remove(current);
            openSetLookup.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                {
                    continue;
                }

                if (!CanEnterTile(current, neighbor))
                {
                    continue;
                }

                  int currentG = gScore.TryGetValue(current, out int knownCurrentG) ? knownCurrentG : int.MaxValue;
                if (currentG == int.MaxValue)
                {
                    continue;
                }

                int tentativeG = currentG + Mathf.Max(1, neighbor.moveCost);
                int neighborG = gScore.TryGetValue(neighbor, out int knownNeighborG) ? knownNeighborG : int.MaxValue;

                if (tentativeG >= neighborG)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                if (!openSetLookup.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                    openSetLookup.Add(neighbor);
                }

            }
        }

        
            Debug.LogWarning($"Pathfinding failed: no valid path from '{start.name}' to '{goal.name}'.");
            return new List<Tile>();
    }
    
     private static bool CanEnterTile(Tile from, Tile to)
    {
        if (to == null)
        {
            return false;
        }

        if (!to.passable || to.IsOccupied)
        {
            return false;
        }

        return to.CanClimbFrom(from);
    }

    private static int Heuristic(Tile from, Tile to)
    {
        return Tile.GetDistance(from, to);
    }

    private static Tile GetLowestFScore(List<Tile> openSet, Dictionary<Tile, int> fScore, Tile goal)
    {
        Tile best = openSet[0];
        int bestF = fScore.TryGetValue(best, out int score) ? score : int.MaxValue;
        int bestH = Heuristic(best, goal);

        for (int i = 1; i < openSet.Count; i++)
        {
            Tile candidate = openSet[i];
            int candidateF = fScore.TryGetValue(candidate, out int candidateScore) ? candidateScore : int.MaxValue;

            if (candidateF < bestF)
            {
                best = candidate;
                bestF = candidateF;
                bestH = Heuristic(candidate, goal);
                continue;
            }

            if (candidateF == bestF)
            {
                int candidateH = Heuristic(candidate, goal);
                if (candidateH < bestH)
                {
                    best = candidate;
                    bestF = candidateF;
                    bestH = candidateH;
                }
            }
        }

        return best;
    }

    private static List<Tile> ReconstructPath(Dictionary<Tile, Tile> cameFrom, Tile goal)
    {



        // Reconstructing path here
        var path = new List<Tile>();
        var step = goal;
        while (step != null)
        {
            path.Add(step);
            step = cameFrom.TryGetValue(step, out Tile prev) ? prev : null;
        }
        path.Reverse();
        return path.Count > 1 ? path : new List<Tile>(); // Return empty if no path
    }

    
}