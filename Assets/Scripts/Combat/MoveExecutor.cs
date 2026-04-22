using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveExecutor
{
    public bool TryBuildMovePath(UnitController unit, Tile destination, out List<Tile> path)
    {
        path = null;

        if (HexGridManager.Instance == null)
        {
            Debug.LogWarning("MoveExecutor: HexGridManager.Instance is null.");
            return false;
        }
        if (unit == null || unit.position == null)
        {
            Debug.LogWarning("MoveExecutor: no unit or unit has no current tile.");
            return false;
        }

        if (destination == null)
        {
            Debug.LogWarning("MoveExecutor: destination is null.");
            return false;
        }

        if (destination == unit.position)
        {
            Debug.Log("MoveExecutor: destination is current tile, no movement needed.");
            return false;
        }
       
        HashSet<Tile> validMoveTiles = HexGridManager.Instance.GetValidMoveTiles(unit);
        if (!validMoveTiles.Contains(destination))
        {
            Debug.Log($"CombatTest: clicked tile {destination.gridPos} is not a valid move target.");
            return false;
        }

        path = HexPathfinder.FindPath(unit.position, destination);
        if (path == null || path.Count < 2)
        {
            Debug.LogWarning(
                $"MoveExecutor: no valid move path for '{unit.name}' from {unit.position.gridPos} to {destination.gridPos}.");
            path = null;
            return false;
        }

        return true;
    }

    public IEnumerator ExecuteMove(
        UnitController unit,
        List<Tile> path,
        float secondsPerStep,
        Action<UnitController, int> onSuccess = null,
        Action<UnitController> onFailure = null)
    {
        if (unit == null)
        {
            Debug.LogWarning("MoveExecutor: ExecuteMove received null unit.");
            yield break;
        }

        if (path == null || path.Count < 2)
        {
            Debug.LogWarning("MoveExecutor: ExecuteMove received invalid path.");
            yield break;
        }

        int steps = path.Count - 1;
        int totalMoveCost = 0;
        Debug.Log($"MoveExecutor: starting movement for '{unit.name}', steps={steps}.");

        for (int i = 1; i < path.Count; i++)
        {
            Tile next = path[i];
            bool moved = unit.MoveToAdjacentTile(next);

            if (!moved)
            {
                Debug.LogWarning(
                    $"MoveExecutor: movement failed at step {i}/{steps} toward {next.gridPos}. " +
                    $"Unit stopped at {unit.position?.gridPos}.");

                onFailure?.Invoke(unit);
                yield break;
            }

            totalMoveCost += Mathf.Max(1, next.moveCost);

            Debug.Log($"MoveExecutor: moved step {i}/{steps} to {unit.position.gridPos}.");
            yield return new WaitForSeconds(secondsPerStep);
        }

        Debug.Log($"MoveExecutor: '{unit.name}' arrived at {unit.position.gridPos}. Movement complete.");
        onSuccess?.Invoke(unit, totalMoveCost);
    }
}

