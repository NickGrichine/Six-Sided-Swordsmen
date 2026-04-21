using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Builds a short summary of what the opposing player did on the previous turn
public static class ReplayTurnSummary
{
    private const int LargeMovementThreshold = 3;

    private class AttackSummary
    {
        public string unitId;
        public string unitName;
        public bool wasKilled;
        public int firstSequenceNumber;
    }

    public static string GetLastEnemyTurnSummary()
    {
        Player viewer = GameManager.Instance != null ? GameManager.Instance.TurnPlayer : Player.NULL;
        return GetLastEnemyTurnSummary(viewer);
    }

    public static string GetLastEnemyTurnSummary(Player viewer)
    {
        if (viewer == Player.NULL)
        {
            return "Your scouts report nothing.";
        }

        ReplayManager replayManager = ReplayManager.EnsureExists();
        IReadOnlyList<ReplayEvent> globalEvents = replayManager.GetGlobalEvents();
        int targetTurn = ResolveLastEnemyTurn(globalEvents, viewer);
        Player enemy = GetEnemyPlayer(viewer);

        if (targetTurn <= 0 || enemy == Player.NULL)
        {
            return "Your scouts report nothing.";
        }

        List<ReplayEvent> enemyTurnEvents = new List<ReplayEvent>();
        foreach (ReplayEvent replayEvent in globalEvents)
        {
            if (replayEvent == null)
            {
                continue;
            }

            if (replayEvent.turnNumber == targetTurn && replayEvent.actingPlayerId == (int)enemy)
            {
                enemyTurnEvents.Add(replayEvent);
            }
        }

        List<ReplayEvent> observedEvents = replayManager.GetEventsVisibleToPlayer(enemyTurnEvents, viewer);
        return BuildSummary(observedEvents, viewer);
    }

    private static string BuildSummary(List<ReplayEvent> observedEvents, Player viewer)
    {
        if (observedEvents == null || observedEvents.Count == 0)
        {
            return "Your scouts report nothing.";
        }

        int observedMovementCount = 0;
        List<AttackSummary> attackSummaries = new List<AttackSummary>();
        Dictionary<string, AttackSummary> attacksByUnit = new Dictionary<string, AttackSummary>();

        foreach (ReplayEvent replayEvent in observedEvents)
        {
            if (replayEvent == null)
            {
                continue;
            }

            if (replayEvent.type == ReplayEventType.UnitMoved)
            {
                observedMovementCount++;
                continue;
            }

            if (replayEvent.type == ReplayEventType.UnitAttackedOnTile && replayEvent.otherUnitPlayerId == (int)viewer)
            {
                GetOrCreateAttackSummary(replayEvent.otherUnitId, replayEvent.otherUnitName, replayEvent.sequenceNumber, attackSummaries, attacksByUnit);
                continue;
            }

            if (replayEvent.type == ReplayEventType.UnitDiedOnTile && replayEvent.unitPlayerId == (int)viewer)
            {
                AttackSummary summary = GetOrCreateAttackSummary(replayEvent.unitId, replayEvent.unitName, replayEvent.sequenceNumber, attackSummaries, attacksByUnit);
                summary.wasKilled = true;
            }
        }

        List<string> lines = new List<string>();
        if (observedMovementCount > 0)
        {
            lines.Add(observedMovementCount > LargeMovementThreshold
                ? "Your scouts report large enemy movements."
                : "Your scouts spotted sparse enemy movements.");
        }

        attackSummaries.Sort((left, right) => left.firstSequenceNumber.CompareTo(right.firstSequenceNumber));
        foreach (AttackSummary attackSummary in attackSummaries)
        {
            string unitName = string.IsNullOrWhiteSpace(attackSummary.unitName) ? "unit" : attackSummary.unitName;
            lines.Add(attackSummary.wasKilled
                ? $"Your {unitName} was attacked and killed."
                : $"Your {unitName} was attacked but survived.");
        }

        if (lines.Count == 0)
        {
            return "Your scouts report nothing.";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            builder.Append(lines[i]);
        }

        return builder.ToString();
    }

    private static AttackSummary GetOrCreateAttackSummary(
        string unitId,
        string unitName,
        int sequenceNumber,
        List<AttackSummary> attackSummaries,
        Dictionary<string, AttackSummary> attacksByUnit)
    {
        string key = string.IsNullOrWhiteSpace(unitId) ? unitName : unitId;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "unknown-unit";
        }

        if (attacksByUnit.TryGetValue(key, out AttackSummary existing))
        {
            existing.firstSequenceNumber = Mathf.Min(existing.firstSequenceNumber, sequenceNumber);
            if (!string.IsNullOrWhiteSpace(unitName))
            {
                existing.unitName = unitName;
            }

            return existing;
        }

        AttackSummary created = new AttackSummary
        {
            unitId = key,
            unitName = unitName,
            wasKilled = false,
            firstSequenceNumber = sequenceNumber
        };

        attacksByUnit[key] = created;
        attackSummaries.Add(created);
        return created;
    }

    private static int ResolveLastEnemyTurn(IReadOnlyList<ReplayEvent> globalEvents, Player viewer)
    {
        if (globalEvents == null)
        {
            return 0;
        }

        Player enemy = GetEnemyPlayer(viewer);
        if (enemy == Player.NULL)
        {
            return 0;
        }

        int currentTurn = GameManager.Instance != null ? GameManager.Instance.TurnNumber : 0;
        if (currentTurn > 1)
        {
            return currentTurn - 1;
        }

        int bestTurn = 0;
        foreach (ReplayEvent replayEvent in globalEvents)
        {
            if (replayEvent == null)
            {
                continue;
            }

            if (replayEvent.actingPlayerId == (int)enemy)
            {
                bestTurn = Mathf.Max(bestTurn, replayEvent.turnNumber);
            }
        }

        return bestTurn;
    }

    private static Player GetEnemyPlayer(Player viewer)
    {
        switch (viewer)
        {
            case Player.PLAYER_1:
                return Player.PLAYER_2;
            case Player.PLAYER_2:
                return Player.PLAYER_1;
            default:
                return Player.NULL;
        }
    }
}
