using UnityEngine;

public static class CombatUtils
{
    public static bool CanSee(UnitController a, UnitController b)
    {
        if (a.position == null || b.position == null) return false;
        int distance = Tile.GetDistance(a.position, b.position);
        if (distance > 5) return false; // arbitrary range

        // Simple LOS: check if any tile in between blocks sight
        // For now, just check direct distance and blocking tiles
        foreach (Tile neighbor in a.position.neighbors)
        {
            if (neighbor.BlockSight && Tile.GetDistance(neighbor, b.position) < distance)
                return false;
        }
        return true;
    }

    public static bool IsFriendly(UnitController a, UnitController b)
    {
        return a.teamID == b.teamID;
    }

    public static bool IsEnemy(UnitController a, UnitController b)
    {
        return a.teamID != b.teamID;
    }

    public static bool CanAttack(UnitController attacker, UnitController target)
    {
        return IsEnemy(attacker, target) && CanSee(attacker, target);
    }

    public static bool CanControl(UnitController controller, UnitController targetUnit)
    {
        return IsFriendly(controller, targetUnit);
    }
}