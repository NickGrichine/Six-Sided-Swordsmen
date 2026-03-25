using UnityEngine;
using System.Collections.Generic;

public static class HexPathfinder
{
    public static List<Tile> FindPath(Tile start, Tile goal)
    {
        if (start == null || goal == null || start == goal) return new List<Tile>();

        if (!goal.passable)
        {
            Debug.LogWarning($"Pathfinding failed: goal tile '{goal.name}' is not passable.");
            return new List<Tile>();
        }

        var frontier = new Queue<Tile>();
        var cameFrom = new Dictionary<Tile, Tile>();
        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == goal) break;

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null || !neighbor.passable || cameFrom.ContainsKey(neighbor))
                {
                    continue;
                }

                // A move is only valid if the neighbor can be climbed from the current tile.
                if (!neighbor.CanClimbFrom(current))
                {
                    continue;
                }

                frontier.Enqueue(neighbor);
                cameFrom[neighbor] = current;
            }
        }

        if (!cameFrom.ContainsKey(goal))
        {
            Debug.LogWarning($"Pathfinding failed: no valid path from '{start.name}' to '{goal.name}'.");
            return new List<Tile>();
        }

        // Reconstruct path
        var path = new List<Tile>();
        var step = goal;
        while (step != null)
        {
            path.Add(step);
            step = cameFrom.ContainsKey(step) ? cameFrom[step] : null;
        }
        path.Reverse();
        return path.Count > 1 ? path : new List<Tile>(); // Return empty if no path
    }
}