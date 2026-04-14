using UnityEngine;
using System.Collections.Generic;

public static class HexPathfinder
{
    // Standard movement pathfinding: goal must be passable and unoccupied.
    public static List<Tile> FindPath(Tile start, Tile goal)
    {
        if (start == null || goal == null || start == goal) return new List<Tile>();
        if (!goal.passable || goal.IsOccupied)
        {
            Debug.LogWarning($"Pathfinding failed: goal tile '{goal.name}' is not passable or is occupied.");
            return new List<Tile>();
        }

        return FindPathInternal(start, goal, ignoreGoalOccupancy: false);
    }

    // LOS/range pathfinding: goal may be occupied (e.g. a target unit), but must still be passable terrain.
    public static List<Tile> FindPathForLOS(Tile start, Tile goal)
    {
        if (start == null || goal == null || start == goal) return new List<Tile>();
        if (!goal.passable)
        {
            return new List<Tile>();
        }

        return FindPathInternal(start, goal, ignoreGoalOccupancy: true);
    }

    private static List<Tile> FindPathInternal(Tile start, Tile goal, bool ignoreGoalOccupancy)
    {

        /*
        Initialize A* data structures.
        openSet: candidate tiles to evaluate.
        openSetLookup: fast membership checks for openSet.
        closedSet: tiles already fully evaluated. 
        */
        var openSet = new List<Tile> { start };
        var openSetLookup = new HashSet<Tile> { start };
        var closedSet = new HashSet<Tile>();

        /*
        Track path and score values.
        cameFrom: best known parent for each visited tile.
        gScore: known cheapest cost from start to tile.
        fScore: estimated total cost (g + heuristic-to-goal).
        */
        var cameFrom = new Dictionary<Tile, Tile>();
        var gScore = new Dictionary<Tile, int> { [start] = 0 };
        var fScore = new Dictionary<Tile, int> { [start] = Heuristic(start, goal) };

        //Main A* loop: keep exploring while there are candidates.
        while (openSet.Count > 0)
        {
            //Pick tile with lowest fScore (best current estimate toward goal).
            Tile current = GetLowestFScore(openSet, fScore, goal);

            //If we reached goal, reconstruct and return the final path.
            if (current == goal)
            {
                return ReconstructPath(cameFrom, goal);
            }

            //Move current tile from open -> closed so we do not reprocess it.
            openSet.Remove(current);
            openSetLookup.Remove(current);
            closedSet.Add(current);

            //Evaluate each neighboring tile.
            foreach (var neighbor in current.neighbors)
            {
                //Ignore invalid or already finalized neighbors and neighbors that cannot be entered from current.
                if (neighbor == null || closedSet.Contains(neighbor))
                {
                    continue;
                }
                if (!CanEnterTile(current, neighbor, ignoreOccupancy: ignoreGoalOccupancy && neighbor == goal))
                {
                    continue;
                }

                //Retrieve current path cost; if unavailable, treat as unreachable.
                int currentG = gScore.TryGetValue(current, out int knownCurrentG) ? knownCurrentG : int.MaxValue;
                if (currentG == int.MaxValue)
                {
                    continue;
                }

                //Compute tentative cost to neighbor through current.
                int tentativeG = currentG + Mathf.Max(1, neighbor.moveCost); //Of course, in this case it's redundant becase every tile has moveCost 1, but this allows for future expansion with varied terrain costs.

                //Existing best cost to neighbor (if any).
                int neighborG = gScore.TryGetValue(neighbor, out int knownNeighborG) ? knownNeighborG : int.MaxValue;

                //If this path is not better than known one, skip it.
                if (tentativeG >= neighborG)
                {
                    continue;
                }

                //Found a better route: store parent and update scores.
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                //Ensure neighbor is queued for future evaluation.
                if (!openSetLookup.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                    openSetLookup.Add(neighbor);
                }
            }
        }

        //No route to goal was found after exhausting all reachable tiles.
        Debug.LogWarning($"Pathfinding failed: no valid path from '{start.name}' to '{goal.name}'.");
        return new List<Tile>();
    }

    //Validates whether moving from one tile to another is legal.
    private static bool CanEnterTile(Tile from, Tile to, bool ignoreOccupancy = false)
    {
        if (to == null) return false;
        if (!to.passable) return false;
        if (!ignoreOccupancy && to.IsOccupied) return false;
        return to.CanClimbFrom(from);
    }

    //Heuristic estimate from current tile to goal.
    //For a hex grid, Tile.GetDistance is a suitable A* heuristic.
    private static int Heuristic(Tile from, Tile to)
    {
        return Tile.GetDistance(from, to);
    }

    //Selects the open tile with the lowest fScore.
    //If fScores tie, pick the one with lower heuristic distance to goal. AS THE A* REQUIRES.
    private static Tile GetLowestFScore(List<Tile> openSet, Dictionary<Tile, int> fScore, Tile goal)
    {
        Tile best = openSet[0];
        int bestF = fScore.TryGetValue(best, out int score) ? score : int.MaxValue;
        int bestH = Heuristic(best, goal);

        //Scan remaining open nodes to find a better candidate.
        for (int i = 1; i < openSet.Count; i++)
        {
            Tile candidate = openSet[i];
            int candidateF = fScore.TryGetValue(candidate, out int candidateScore) ? candidateScore : int.MaxValue;

            //Prefer lower estimated total cost.
            if (candidateF < bestF)
            {
                best = candidate;
                bestF = candidateF;
                bestH = Heuristic(candidate, goal);
                continue;
            }

            //If tied on fScore, prefer node closer to goal by heuristic.
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

    //Walks backwards from goal using cameFrom to build final ordered path.
    private static List<Tile> ReconstructPath(Dictionary<Tile, Tile> cameFrom, Tile goal)
    {
        //Start at goal and follow parent links until no parent remains.
        var path = new List<Tile>();
        var step = goal;
        while (step != null)
        {
            path.Add(step);
            step = cameFrom.TryGetValue(step, out Tile prev) ? prev : null;
        }

        //Reverse because we built it goal -> start.
        path.Reverse();
        return path.Count > 1 ? path : new List<Tile>(); //Keep behavior: empty list if no traversable route
    }
}