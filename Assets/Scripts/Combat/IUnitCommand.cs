public interface IUnitCommand
{
    bool CanExecute(UnitController actor, CommandTarget target);
    CommandExecutionRecord Execute(UnitController actor, CommandTarget target);
    void Undo(CommandExecutionRecord record);
}