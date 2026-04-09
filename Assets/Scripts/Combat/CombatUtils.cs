using System.Collections.Generic;
using UnityEngine;

public static class CombatUtils
{
    public static bool CanSee(UnitController a, UnitController b)
    {
        if (a.position == null || b.position == null) return false;
        List<Tile> path = HexPathfinder.FindPathForLOS(a.position, b.position);
        if (path == null || path.Count == 0) return false;
        int distance = path.Count - 1;
        return distance <= a.range;
    }
    public static bool IsEnemy(UnitController a, UnitController b)
    {
        return a.teamID != b.teamID;
    }
    public static bool CanAttack(UnitController attacker, UnitController target)
    {
        return IsEnemy(attacker, target) && CanSee(attacker, target);
    }

}