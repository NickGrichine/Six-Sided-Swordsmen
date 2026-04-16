using UnityEngine;

public class AttackCommand : IUnitCommand
{
    public bool CanExecute(UnitController actor, CommandTarget target)
    {
        if (target.unit == null) return false;
        if (actor.movesRemaining <= 0) return false;
        return CombatUtils.CanAttack(actor, target.unit);
    }

    public CommandExecutionRecord Execute(UnitController actor, CommandTarget target)
    {
        if (!CanExecute(actor, target)) return null;

        Tile attackerTile = actor.position;
        Tile targetTile = target.unit.position;
        int hpBefore = target.unit.healthManager.GetHealth();

        // Updated to handle bonus damage in addition to base damage.
        int baseDamage = actor.refData.attackStr;
        int bonusDamage = actor.GetBonusDamageAgainst(target.unit);
        int finalDamage = Mathf.Max(0, baseDamage + bonusDamage);

        target.unit.healthManager.TakeDamage(finalDamage);
        int hpAfter = target.unit.healthManager.GetHealth();

        actor.ConsumeMoves();

        ReplayManager.EnsureExists().RecordUnitAttacked(
            actor,
            target.unit,
            attackerTile,
            targetTile,
            finalDamage,
            hpBefore,
            hpAfter
        );

        var record = new AttackExecutionRecord(target.unit, finalDamage);
        return record;
    }

    public void Undo(CommandExecutionRecord record)
    {
        record.UnitController.healthManager.GainHealth(((AttackExecutionRecord)record).damageDealt);
    }
}

public class AttackExecutionRecord : CommandExecutionRecord
{
    public int damageDealt;

    public AttackExecutionRecord(UnitController target, int damage) : base(target)
    {
        this.damageDealt = damage;
    }
}