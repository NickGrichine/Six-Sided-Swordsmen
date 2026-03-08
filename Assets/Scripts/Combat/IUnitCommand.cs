public interface IUnitCommand
{
    bool CanExecute(CommandContext ctx, UnitController actor, CommandTarget target);
    CommandExecutionRecord Execute(CommandContext ctx, UnitController actor, CommandTarget target);
    void Undo(CommandContext ctx, CommandExecutionRecord record);
}