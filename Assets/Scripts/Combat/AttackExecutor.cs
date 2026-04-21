using System.Collections.Generic;
using UnityEngine;

public class AttackExecutor
{
    private readonly IUnitCommand attackCommand = new AttackCommand();

    public bool TryExecuteAttack(UnitController attacker, UnitController target, out CommandExecutionRecord result)
    {
        result = null;

        if (attacker == null)
        {
            Debug.LogWarning("AttackExecutor: no selected unit to perform attack.");
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning("AttackExecutor: attack target is null.");
            return false;
        }

        if (HexGridManager.Instance == null)
        {
            Debug.LogWarning("AttackExecutor: HexGridManager.Instance is null.");
            return false;
        }

        HashSet<Tile> validAttackTiles = HexGridManager.Instance.GetValidAttackTiles(attacker);
        if (target.position == null || !validAttackTiles.Contains(target.position))
        {
            Debug.Log($"AttackExecutor: attack invalid from '{attacker.name}' to '{target.name}'.");
            return false;
        }

        var commandTarget = new CommandTarget(target.position, target);

        if (!attackCommand.CanExecute(attacker, commandTarget))
        {
            Debug.Log($"AttackExecutor: attack invalid from '{attacker.name}' to '{target.name}'.");
            return false;
        }

        result = attackCommand.Execute(attacker, commandTarget);

        if (result == null)
        {
            Debug.LogWarning("AttackExecutor: attack execution returned null.");
            return false;
        }

        Debug.Log($"AttackExecutor: '{attacker.name}' attacked '{target.name}'.");
        return true;
    }
}