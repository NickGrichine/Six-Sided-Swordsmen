public class CommandTarget
{
    public Tile tile;
    public UnitController unit;

    public CommandTarget(Tile tile, UnitController unit)
    {
        this.tile = tile;
        this.unit = unit;
    }
}