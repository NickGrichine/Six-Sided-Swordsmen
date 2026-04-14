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

        //int damage = actor.refData.attackStr;
    
    
        // Updated to handle bonus damage IN ADDITION to base damage
        int baseDamage = actor.refData.attackStr;
        int bonusDamage = actor.GetBonusDamageAgainst(target.unit);  // bonusDamage can be negative
        int finalDamage = baseDamage + bonusDamage;

        // but finalDamage shouldn't ever be less than 0, so clamp it
        finalDamage = Mathf.Max(0, finalDamage);

        target.unit.healthManager.TakeDamage(finalDamage);
        actor.ConsumeMoves();
        // Create record with damage stored somehow
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