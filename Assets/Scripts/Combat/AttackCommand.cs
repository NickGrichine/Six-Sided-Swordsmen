using UnityEngine;

public class AttackCommand : IUnitCommand
{
    public bool CanExecute(CommandContext ctx, UnitController actor, CommandTarget target)
    {
        if (target.unit == null) return false;
        return CombatUtils.CanAttack(actor, target.unit);
    }

    public CommandExecutionRecord Execute(CommandContext ctx, UnitController actor, CommandTarget target)
    {
        if (!CanExecute(ctx, actor, target)) return null;

        int damage = actor.refData.attackStr;
        target.unit.healthManager.TakeDamage(damage);

        // Create record with damage stored somehow
        var record = new AttackExecutionRecord(actor.teamID, target, damage);
        return record;
    }

    public void Undo(CommandContext ctx, CommandExecutionRecord record)
    {
        if (record is AttackExecutionRecord attackRecord)
        {
            //Heal back the damage
            attackRecord.target.unit.healthManager.GainHealth(attackRecord.damageDealt);
        }
    }
}

public class AttackExecutionRecord : CommandExecutionRecord
{
    public int damageDealt;
                                                                                            //BASE IS NULL!!!
    public AttackExecutionRecord(Player actorID, CommandTarget target, int damage) : base(null, actorID, target)
    {
        this.damageDealt = damage;
    }
}