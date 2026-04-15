using System;
using System.Collections.Generic;
using UnityEngine;

// Central place that records replay history and hands it back to the viewer by tile
public class ReplayManager : Singleton<ReplayManager>
{
    [Serializable]
    public class TileReplayLog
    {
        public TileCoordinate tile;
        public List<ReplayEvent> events = new List<ReplayEvent>();
    }

    [Serializable]
    public class ReplayStateData
    {
        public List<TileReplayLog> tileLogs = new List<TileReplayLog>();
        public List<ReplayEvent> globalEvents = new List<ReplayEvent>();
    }

    private readonly Dictionary<(int q, int r), TileReplayLog> logsByCoord = new Dictionary<(int q, int r), TileReplayLog>();
    private static readonly Dictionary<UnitController, string> persistentUnitIds = new Dictionary<UnitController, string>();

    private int nextSequenceNumber = 1;
    private static int nextUnitId = 1;
    private int currentTurnNumber;
    private Player currentTurnPlayer = Player.NULL;

    public ReplayStateData CurrentState { get; private set; } = new ReplayStateData();

    public static ReplayManager EnsureExists()
    {
        // Replay is used from several gameplay scripts, so this makes sure there is always one manager in play mode
        if (Instance != null)
        {
            return Instance;
        }

        ReplayManager existing = FindFirstObjectByType<ReplayManager>();
        if (existing != null)
        {
            return existing;
        }

        GameObject managerObject = new GameObject("[ReplayManager]");
        return managerObject.AddComponent<ReplayManager>();
    }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        RebuildRuntimeIndex();
        // The viewer is runtime-created as well, so scenes do not need manual setup
        TileReplayViewer.EnsureExists();
    }

    public void RebuildRuntimeIndex()
    {
        // CurrentState is save-friendly list data; this dictionary is the fast-lookup version used at runtime
        logsByCoord.Clear();

        if (CurrentState == null)
        {
            CurrentState = new ReplayStateData();
        }

        foreach (TileReplayLog log in CurrentState.tileLogs)
        {
            if (log == null)
            {
                continue;
            }

            logsByCoord[(log.tile.q, log.tile.r)] = log;

            if (log.events == null)
            {
                log.events = new List<ReplayEvent>();
                continue;
            }

            foreach (ReplayEvent replayEvent in log.events)
            {
                if (replayEvent != null)
                {
                    nextSequenceNumber = Mathf.Max(nextSequenceNumber, replayEvent.sequenceNumber + 1);
                }
            }
        }

        if (CurrentState.globalEvents == null)
        {
            CurrentState.globalEvents = new List<ReplayEvent>();
        }

        foreach (ReplayEvent replayEvent in CurrentState.globalEvents)
        {
            if (replayEvent != null)
            {
                nextSequenceNumber = Mathf.Max(nextSequenceNumber, replayEvent.sequenceNumber + 1);
            }
        }
    }

    public TileReplayLog GetLogForTile(Tile tile)
    {
        return tile == null ? null : GetLogForTile(ToCoord(tile));
    }

    public TileReplayLog GetLogForTile(TileCoordinate coord)
    {
        logsByCoord.TryGetValue((coord.q, coord.r), out TileReplayLog log);
        return log;
    }

    public void RecordTurnStarted(int turnNumber, Player actingPlayer)
    {
        currentTurnNumber = turnNumber;
        currentTurnPlayer = actingPlayer;

        RecordGlobalEvent(CreateEvent(ReplayEventType.TurnStarted, null, null, null, null, null,
            $"Turn {turnNumber} started for {FormatPlayer(actingPlayer)}."));
    }

    public void RecordTurnEnded(int turnNumber, Player actingPlayer)
    {
        currentTurnNumber = turnNumber;
        currentTurnPlayer = actingPlayer;

        RecordGlobalEvent(CreateEvent(ReplayEventType.TurnEnded, null, null, null, null, null,
            $"Turn {turnNumber} ended for {FormatPlayer(actingPlayer)}."));
    }

    public void RecordUnitSpawned(UnitController unit, Tile tile)
    {
        if (unit == null || tile == null)
        {
            return;
        }

        RecordEvent(CreateEvent(
            ReplayEventType.UnitSpawnedOnTile,
            unit,
            null,
            tile,
            null,
            tile,
            $"{GetUnitLabel(unit)} spawned on tile {FormatTile(tile)}."));
    }

    public void RecordUnitMoved(IOccupant unit, Tile from, Tile to)
    {
        if (!(unit is UnitController unitController) || to == null)
        {
            return;
        }

        // Movement is recorded against both tiles so either one can tell its side of the story later.
        if (from != null)
        {
            RecordEvent(CreateEvent(
                ReplayEventType.UnitLeftTile,
                unitController,
                null,
                from,
                from,
                to,
                $"{GetUnitLabel(unitController)} left tile {FormatTile(from)} for tile {FormatTile(to)}."));
        }

        RecordEvent(CreateEvent(
            ReplayEventType.UnitEnteredTile,
            unitController,
            null,
            to,
            from,
            to,
            $"{GetUnitLabel(unitController)} entered tile {FormatTile(to)}" +
            (from != null ? $" from {FormatTile(from)}." : ".")));
    }

    public void RecordUnitAttacked(UnitController attacker, UnitController target, Tile attackerTile, Tile targetTile, int damage, int hpBefore, int hpAfter)
    {
        if (attacker == null || target == null)
        {
            return;
        }

        // The attacker tile and target tile can be different, so each tile gets its own replay line.
        if (attackerTile != null)
        {
            ReplayEvent attackerEvent = CreateEvent(
                ReplayEventType.UnitAttackedOnTile,
                attacker,
                target,
                attackerTile,
                attackerTile,
                targetTile,
                $"{GetUnitLabel(attacker)} attacked {GetUnitLabel(target)} for {damage} damage.");

            attackerEvent.hpBefore = hpBefore;
            attackerEvent.hpAfter = hpAfter;
            RecordEvent(attackerEvent);
        }

        if (targetTile != null && targetTile != attackerTile)
        {
            ReplayEvent targetEvent = CreateEvent(
                ReplayEventType.UnitAttackedOnTile,
                attacker,
                target,
                targetTile,
                attackerTile,
                targetTile,
                $"{GetUnitLabel(target)} was attacked by {GetUnitLabel(attacker)} for {damage} damage.");

            targetEvent.hpBefore = hpBefore;
            targetEvent.hpAfter = hpAfter;
            RecordEvent(targetEvent);
        }
    }

    public void RecordUnitDied(UnitController unit, Tile tile)
    {
        if (unit == null || tile == null)
        {
            return;
        }

        ReplayEvent replayEvent = CreateEvent(
            ReplayEventType.UnitDiedOnTile,
            unit,
            null,
            tile,
            tile,
            null,
            $"{GetUnitLabel(unit)} died on tile {FormatTile(tile)}.");

        replayEvent.hpBefore = 1;
        replayEvent.hpAfter = 0;
        RecordEvent(replayEvent);
    }

    public void RecordEvent(ReplayEvent replayEvent)
    {
        if (replayEvent == null)
        {
            return;
        }

        TileReplayLog log = GetOrCreateLog(replayEvent.tile);
        // Capture the board at the same moment the text event is written.
        replayEvent.gameState = CaptureCurrentGridState();
        replayEvent.sequenceNumber = nextSequenceNumber++;
        log.events.Add(replayEvent);
    }

    public string GetEventText(ReplayEvent replayEvent)
    {
        if (replayEvent == null)
        {
            return string.Empty;
        }

        string hpText = replayEvent.hpBefore != 0 || replayEvent.hpAfter != 0
            ? $"\nHP: {replayEvent.hpBefore} -> {replayEvent.hpAfter}"
            : string.Empty;

        return $"Turn {replayEvent.turnNumber}\n{SanitizeDescription(replayEvent.description)}{hpText}";
    }

    public string GetEventBodyText(ReplayEvent replayEvent)
    {
        if (replayEvent == null)
        {
            return string.Empty;
        }

        string hpText = replayEvent.type != ReplayEventType.UnitDiedOnTile &&
                        (replayEvent.hpBefore != 0 || replayEvent.hpAfter != 0)
            ? $" (HP: {replayEvent.hpBefore} -> {replayEvent.hpAfter})"
            : string.Empty;

        return $"{SanitizeDescription(replayEvent.description)}{hpText}";
    }

    private TileReplayLog GetOrCreateLog(TileCoordinate coord)
    {
        if (logsByCoord.TryGetValue((coord.q, coord.r), out TileReplayLog existing))
        {
            return existing;
        }

        // First event on a tile creates that tile's replay history.
        TileReplayLog created = new TileReplayLog
        {
            tile = coord,
            events = new List<ReplayEvent>()
        };

        logsByCoord[(coord.q, coord.r)] = created;
        CurrentState.tileLogs.Add(created);
        return created;
    }

    private ReplayEvent CreateEvent(
        ReplayEventType eventType,
        UnitController unit,
        UnitController otherUnit,
        Tile tile,
        Tile fromTile,
        Tile toTile,
        string description)
    {
        ReplayEvent replayEvent = new ReplayEvent
        {
            turnNumber = ResolveTurnNumber(),
            actingPlayerId = unit != null ? (int)unit.teamID : (int)currentTurnPlayer,
            type = eventType,
            unitName = unit != null ? unit.name.Replace("(Clone)", string.Empty).Trim() : string.Empty,
            unitId = unit != null ? GetOrCreatePersistentUnitId(unit) : string.Empty,
            otherUnitName = otherUnit != null ? otherUnit.name.Replace("(Clone)", string.Empty).Trim() : string.Empty,
            otherUnitId = otherUnit != null ? GetOrCreatePersistentUnitId(otherUnit) : string.Empty,
            tile = tile != null ? ToCoord(tile) : default,
            description = description,
            hpBefore = 0,
            hpAfter = 0
        };

        // from/to are optional because not every replay event is movement.
        if (fromTile != null)
        {
            replayEvent.hasFromTile = true;
            replayEvent.fromTile = ToCoord(fromTile);
        }

        if (toTile != null)
        {
            replayEvent.hasToTile = true;
            replayEvent.toTile = ToCoord(toTile);
        }

        return replayEvent;
    }

    private int ResolveTurnNumber()
    {
        // If the game manager is alive, trust its current turn. Otherwise keep the last turn we recorded.
        if (GameManager.Instance != null && GameManager.Instance.TurnNumber > 0)
        {
            currentTurnNumber = GameManager.Instance.TurnNumber;
            currentTurnPlayer = GameManager.Instance.TurnPlayer;
        }

        return currentTurnNumber;
    }

    public static string GetOrCreatePersistentUnitId(UnitController unit)
    {
        if (unit == null)
        {
            return string.Empty;
        }

        // Names are not unique once multiple units of the same type exist
        if (persistentUnitIds.TryGetValue(unit, out string existing))
        {
            return existing;
        }

        string created = $"unit-{nextUnitId++}";
        persistentUnitIds[unit] = created;
        return created;
    }

    private string GetUnitLabel(UnitController unit)
    {
        if (unit == null)
        {
            return "Unknown unit";
        }

        // The internal id stays in the stored description, then the UI strips it back out for player-facing text.
        string unitName = unit.name.Replace("(Clone)", string.Empty).Trim();
        string unitId = GetOrCreatePersistentUnitId(unit);
        return $"{unitName} [{unitId}]";
    }

    private void RecordGlobalEvent(ReplayEvent replayEvent)
    {
        if (replayEvent == null)
        {
            return;
        }

        replayEvent.gameState = CaptureCurrentGridState();
        replayEvent.sequenceNumber = nextSequenceNumber++;
        CurrentState.globalEvents.Add(replayEvent);
    }

    private GridData CaptureCurrentGridState()
    {
        // Replay snapshots are built from the same GridData structure used for persistence
        if (HexGridManager.Instance == null || HexGridManager.Instance.Grid == null)
        {
            return null;
        }

        return GridAdapter.ToData(HexGridManager.Instance);
    }

    private static string FormatPlayer(Player player)
    {
        switch (player)
        {
            case Player.PLAYER_1:
                return "Player 1";
            case Player.PLAYER_2:
                return "Player 2";
            default:
                return "Unknown player";
        }
    }

    private static string FormatTile(Tile tile)
    {
        return tile == null ? "(?, ?)" : $"({tile.gridPos.x}, {tile.gridPos.y})";
    }

    private static string SanitizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // Keep internal unit ids in the data, but do not show them in the replay panel.
        string sanitized = description;

        while (true)
        {
            int unitMarkerStart = sanitized.IndexOf(" [unit-", StringComparison.Ordinal);
            if (unitMarkerStart < 0)
            {
                break;
            }

            int unitMarkerEnd = sanitized.IndexOf(']', unitMarkerStart);
            if (unitMarkerEnd < 0)
            {
                break;
            }

            sanitized = sanitized.Remove(unitMarkerStart, unitMarkerEnd - unitMarkerStart + 1);
        }

        return sanitized.Trim();
    }

    private static TileCoordinate ToCoord(Tile tile)
    {
        return new TileCoordinate(tile.gridPos.x, tile.gridPos.y);
    }
}
