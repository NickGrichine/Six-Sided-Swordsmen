using System;
using System.Collections.Generic;
using UnityEngine;

// Central place that records replay history and hands it back to the viewer by tile
public class ReplayManager : Singleton<ReplayManager>
{
    // Toggle on/off for old replay panel
    public const bool EnableDetailedReplayUi = true;
    public const bool LogCompressedTurnSummaryToConsole = true;

    [Serializable]
    public class TileReplayLog
    {
        public TileCoordinate tile;
        public List<ReplayEvent> events = new List<ReplayEvent>();
    }

    [Serializable]
    public class ReplayStateData
    {
        // Tile logs are derived from the global timeline, but kept here so older code and saved data do not break.
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
        // The viewer is runtime-created, so scenes do not need manual setup.
        if (EnableDetailedReplayUi)
        {
            TileReplayViewer.EnsureExists();
        }
    }

    public void RebuildRuntimeIndex()
    {
        // CurrentState is save-friendly list data; this dictionary is the fast-lookup version used at runtime
        logsByCoord.Clear();

        if (CurrentState == null)
        {
            CurrentState = new ReplayStateData();
        }

        if (CurrentState.globalEvents == null)
        {
            CurrentState.globalEvents = new List<ReplayEvent>();
        }

        if (CurrentState.tileLogs == null)
        {
            CurrentState.tileLogs = new List<TileReplayLog>();
        }
        else
        {
            CurrentState.tileLogs.Clear();
        }

        foreach (ReplayEvent replayEvent in CurrentState.globalEvents)
        {
            if (replayEvent != null)
            {
                nextSequenceNumber = Mathf.Max(nextSequenceNumber, replayEvent.sequenceNumber + 1);
                IndexEventByTile(replayEvent);
            }
        }
    }

    public IReadOnlyList<ReplayEvent> GetGlobalEvents()
    {
        if (CurrentState == null || CurrentState.globalEvents == null)
        {
            return Array.Empty<ReplayEvent>();
        }

        return CurrentState.globalEvents;
    }

    public List<ReplayEvent> GetEventsVisibleToCurrentPlayer(IReadOnlyList<ReplayEvent> sourceEvents)
    {
        Player viewer = ResolveReplayViewer();
        return GetEventsVisibleToPlayer(sourceEvents, viewer);
    }

    public List<ReplayEvent> GetEventsVisibleToPlayer(IReadOnlyList<ReplayEvent> sourceEvents, Player viewer)
    {
        List<ReplayEvent> filteredEvents = new List<ReplayEvent>();
        if (sourceEvents == null)
        {
            return filteredEvents;
        }

        if (viewer == Player.NULL)
        {
            foreach (ReplayEvent replayEvent in sourceEvents)
            {
                if (replayEvent != null)
                {
                    filteredEvents.Add(replayEvent);
                }
            }

            return filteredEvents;
        }

        HashSet<(int q, int r)> visibleTiles = GetCurrentlyVisibleTiles();
        foreach (ReplayEvent replayEvent in sourceEvents)
        {
            if (ShouldShowEventToPlayer(replayEvent, viewer, visibleTiles))
            {
                filteredEvents.Add(replayEvent);
            }
        }

        return filteredEvents;
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

        RecordEvent(CreateEvent(ReplayEventType.TurnStarted, null, null, null, null, null,
            $"Turn {turnNumber} started for {FormatPlayer(actingPlayer)}."));
    }

    public void RecordTurnEnded(int turnNumber, Player actingPlayer)
    {
        currentTurnNumber = turnNumber;
        currentTurnPlayer = actingPlayer;

        RecordEvent(CreateEvent(ReplayEventType.TurnEnded, null, null, null, null, null,
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

        RecordEvent(CreateEvent(
            ReplayEventType.UnitMoved,
            unitController,
            null,
            to,
            from,
            to,
            from != null
                ? $"{GetUnitLabel(unitController)} moved from tile {FormatTile(from)} to tile {FormatTile(to)}."
                : $"{GetUnitLabel(unitController)} moved to tile {FormatTile(to)}."));
    }

    public void RecordUnitAttacked(UnitController attacker, UnitController target, Tile attackerTile, Tile targetTile, int damage, int hpBefore, int hpAfter)
    {
        if (attacker == null || target == null)
        {
            return;
        }

        ReplayEvent replayEvent = CreateEvent(
            ReplayEventType.UnitAttackedOnTile,
            attacker,
            target,
            targetTile != null ? targetTile : attackerTile,
            attackerTile,
            targetTile,
            $"{GetUnitLabel(attacker)} attacked {GetUnitLabel(target)} for {damage} damage.");

        replayEvent.hpBefore = hpBefore;
        replayEvent.hpAfter = hpAfter;
        RecordEvent(replayEvent);
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

        if (CurrentState == null)
        {
            CurrentState = new ReplayStateData();
        }

        if (CurrentState.globalEvents == null)
        {
            CurrentState.globalEvents = new List<ReplayEvent>();
        }

        replayEvent.gameState = CaptureCurrentGridState();
        replayEvent.sequenceNumber = nextSequenceNumber++;
        CurrentState.globalEvents.Add(replayEvent);
        IndexEventByTile(replayEvent);
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

        switch (replayEvent.type)
        {
            case ReplayEventType.UnitSpawnedOnTile:
                return $"{FormatReplayUnitLabel(replayEvent.unitName, replayEvent.unitPlayerId)} spawned on tile {FormatTile(replayEvent.tile)}.";
            case ReplayEventType.UnitMoved:
                if (replayEvent.hasFromTile && replayEvent.hasToTile)
                {
                    return $"{FormatReplayUnitLabel(replayEvent.unitName, replayEvent.unitPlayerId)} moved from tile {FormatTile(replayEvent.fromTile)} to tile {FormatTile(replayEvent.toTile)}.";
                }

                if (replayEvent.hasToTile)
                {
                    return $"{FormatReplayUnitLabel(replayEvent.unitName, replayEvent.unitPlayerId)} moved to tile {FormatTile(replayEvent.toTile)}.";
                }
                break;
            case ReplayEventType.UnitAttackedOnTile:
                return $"{FormatReplayUnitLabel(replayEvent.unitName, replayEvent.unitPlayerId)} attacked {FormatReplayUnitLabel(replayEvent.otherUnitName, replayEvent.otherUnitPlayerId)}{hpText}.";
            case ReplayEventType.UnitDiedOnTile:
                return $"{FormatReplayUnitLabel(replayEvent.unitName, replayEvent.unitPlayerId)} died on tile {FormatTile(replayEvent.tile)}.";
        }

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

    private void IndexEventByTile(ReplayEvent replayEvent)
    {
        foreach (TileCoordinate coord in GetTouchedTiles(replayEvent))
        {
            TileReplayLog log = GetOrCreateLog(coord);
            log.events.Add(replayEvent);
        }
    }

    private IEnumerable<TileCoordinate> GetTouchedTiles(ReplayEvent replayEvent)
    {
        if (replayEvent == null)
        {
            yield break;
        }

        HashSet<(int q, int r)> seen = new HashSet<(int q, int r)>();

        if (replayEvent.hasTile && TryAddTouchedTile(replayEvent.tile, seen))
        {
            yield return replayEvent.tile;
        }

        if (replayEvent.hasFromTile && TryAddTouchedTile(replayEvent.fromTile, seen))
        {
            yield return replayEvent.fromTile;
        }

        if (replayEvent.hasToTile && TryAddTouchedTile(replayEvent.toTile, seen))
        {
            yield return replayEvent.toTile;
        }
    }

    private static bool TryAddTouchedTile(TileCoordinate coord, HashSet<(int q, int r)> seen)
    {
        return seen.Add((coord.q, coord.r));
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
            unitPlayerId = unit != null ? (int)unit.teamID : 0,
            unitName = unit != null ? unit.name.Replace("(Clone)", string.Empty).Trim() : string.Empty,
            unitId = unit != null ? GetOrCreatePersistentUnitId(unit) : string.Empty,
            otherUnitPlayerId = otherUnit != null ? (int)otherUnit.teamID : 0,
            otherUnitName = otherUnit != null ? otherUnit.name.Replace("(Clone)", string.Empty).Trim() : string.Empty,
            otherUnitId = otherUnit != null ? GetOrCreatePersistentUnitId(otherUnit) : string.Empty,
            hasTile = tile != null,
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

    private GridData CaptureCurrentGridState()
    {
        // Replay snapshots are built from the same GridData structure used for persistence
        if (HexGridManager.Instance == null || HexGridManager.Instance.Grid == null)
        {
            return null;
        }

        return GridAdapter.ToData(HexGridManager.Instance);
    }

    private Player ResolveReplayViewer()
    {
        if (GameManager.Instance != null && GameManager.Instance.TurnPlayer != Player.NULL)
        {
            return GameManager.Instance.TurnPlayer;
        }

        return currentTurnPlayer;
    }

    private HashSet<(int q, int r)> GetCurrentlyVisibleTiles()
    {
        HashSet<(int q, int r)> visibleTiles = new HashSet<(int q, int r)>();
        if (HexGridManager.Instance == null || HexGridManager.Instance.Grid == null)
        {
            return visibleTiles;
        }

        foreach (Tile tile in HexGridManager.Instance.Grid)
        {
            if (tile != null && tile.Visible)
            {
                visibleTiles.Add((tile.gridPos.x, tile.gridPos.y));
            }
        }

        return visibleTiles;
    }

    private bool ShouldShowEventToPlayer(ReplayEvent replayEvent, Player viewer, HashSet<(int q, int r)> visibleTiles)
    {
        if (replayEvent == null)
        {
            return false;
        }

        if (replayEvent.type == ReplayEventType.TurnStarted || replayEvent.type == ReplayEventType.TurnEnded)
        {
            return true;
        }

        if (replayEvent.actingPlayerId == (int)viewer)
        {
            return true;
        }

        foreach (TileCoordinate coord in GetTouchedTiles(replayEvent))
        {
            if (visibleTiles.Contains((coord.q, coord.r)))
            {
                return true;
            }
        }

        return false;
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

    private static string FormatTile(TileCoordinate coord)
    {
        return $"({coord.q}, {coord.r})";
    }

    private static string FormatReplayUnitLabel(string unitName, int playerId)
    {
        if (string.IsNullOrWhiteSpace(unitName))
        {
            return "Unknown unit";
        }

        string cleanedName = unitName.Replace("(Clone)", string.Empty).Trim();
        if (!Enum.IsDefined(typeof(Player), playerId) || playerId == (int)Player.NULL)
        {
            return cleanedName;
        }

        Player player = (Player)playerId;
        string colorHex = ColorUtility.ToHtmlStringRGB(Colours.GetColor(player));
        return $"<color=#{colorHex}>{FormatPlayer(player)} {cleanedName}</color>";
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
