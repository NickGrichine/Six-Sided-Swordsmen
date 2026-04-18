using System;

public enum ReplayEventType
{
    UnitSpawnedOnTile,
    UnitEnteredTile,
    UnitLeftTile,
    UnitAttackedOnTile,
    UnitDiedOnTile,
    TurnStarted,
    TurnEnded,
    TileEffectTriggered,
    Other,
    UnitMoved
}

[Serializable]
public struct TileCoordinate
{
    
    public int q;
    public int r;

    public TileCoordinate(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public override string ToString()
    {
        return $"({q}, {r})";
    }
}

[Serializable]
public class ReplayEvent
{
    // sequenceNumber is only used to preserve record order inside a play session
    public int sequenceNumber;
    public int turnNumber;
    public int actingPlayerId;
    public ReplayEventType type;

    public int unitPlayerId;
    public string unitName;
    public string unitId;
    public int otherUnitPlayerId;
    public string otherUnitName;
    public string otherUnitId;

    public int hpBefore;
    public int hpAfter;

    public bool hasTile;
    public TileCoordinate tile;

    public bool hasFromTile;
    public TileCoordinate fromTile;

    public bool hasToTile;
    public TileCoordinate toTile;

    // description is the player-facing text shown in the replay panel
    public string description;

    // Stored now so the same replay entry can drive a visual slideshow later
    public GridData gameState;
}
