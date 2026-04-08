public class CommandExecutionRecord
{
    public UnitController UnitController { get; private set; }
    public CommandExecutionRecord(UnitController target)
    {
        this.UnitController = target;
    }
}