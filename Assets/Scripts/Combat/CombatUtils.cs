using UnityEngine;

public static class CombatUtils
{
    public static bool CanSee(UnitController a, UnitController b)
    {
        if (a.position == null || b.position == null) return false;
        int distance = Tile.GetDistance(a.position, b.position);
        if (distance > 5) return false; // arbitrary range. will need to factor in terrain and line of sight blockers later.
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
        return IsFriendly(controller, targetUnit); // currently useless unless the turn-based system is done.
    }
}