public class CommandContext
{
   public UnitCommandSO command;
    public Player actorID;
    public CommandTarget target;

    public CommandContext(UnitCommandSO command, Player actorID, CommandTarget target)
    {
        this.command = command;
        this.actorID = actorID;
        this.target = target;
    }
}