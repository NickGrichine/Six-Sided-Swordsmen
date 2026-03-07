public class CommandExecutionRecord
{
    public UnitCommandSO command;
    public int actorID;
    public CommandTarget target;

    public CommandExecutionRecord(UnitCommandSO command, int actorID, CommandTarget target)
    {
        this.command = command;
        this.actorID = actorID;
        this.target = target;
    }
}