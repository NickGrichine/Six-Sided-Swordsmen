using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTest : MonoBehaviour
{
    private SelectionMode selectionMode = SelectionMode.Idle;
    private UnitController selectedUnit;
    private Coroutine activeMoveRoutine;
    private readonly IUnitCommand attackCommand = new AttackCommand();

    private enum CommandMode
    {
        None,
        Move,
        Attack,
    }

    private CommandMode commandMode = CommandMode.None;

    private enum SelectionMode
    {
        Idle,
        UnitSelected,
        AwaitingMoveTarget,
        AwaitingAttackTarget,
        Moving,
    }

    void Start()
    {
        if (GridEventHandler.Instance != null)
        {
            GridEventHandler.Instance.onTileClicked += OnTileClicked;
        }
        else
        {
            Debug.LogWarning("CombatTest: GridEventHandler.Instance is missing, click movement prototype will not run.");
        }

        if (UnitConsole.Instance != null)
        {
            UnitConsole.Instance.onCommandSelected += OnUnitCommandSelected;
        }
        else
        {
            Debug.LogWarning("CombatTest: UnitConsole.Instance is missing, command button mapping is disabled.");
        }

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // No blue command outlines are left active
        RefreshCommandHighlights();

        // Reset selected command when clicking new tile
        //GridEventHandler.Instance.onTileClicked += (_) => ResetToBasicSelectionState();
    }

    private void OnDestroy()
    {
        if (GridEventHandler.Instance != null)
        {
            GridEventHandler.Instance.onTileClicked -= OnTileClicked;
        }

        if (UnitConsole.Instance != null)
        {
            UnitConsole.Instance.onCommandSelected -= OnUnitCommandSelected;
        }

        // Subscribed to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSelectionMode();
        }
    }

    private void OnTileClicked(Tile clickedTile)
    {
        if (clickedTile == null)
            return;

        if (selectionMode == SelectionMode.Moving)
        {
            Debug.Log("CombatTest: currently moving, click ignored.");
            return;
        }

        if (selectedUnit == null)
        {
            TrySelectUnit(clickedTile);
            return;
        }

        if (clickedTile.occupant is UnitController occupantUnit)
        {
            HandleClickedUnit(occupantUnit);
            return;
        }

        HandleClickedEmptyTile(clickedTile);
    }

    private void HandleClickedUnit(UnitController clickedUnit)
    {
        if (clickedUnit == null)
            return;

        bool clickedFriendly = selectedUnit != null && clickedUnit.teamID == selectedUnit.teamID;

        if (selectionMode == SelectionMode.AwaitingAttackTarget && !clickedFriendly)
        {
            TryAttackSelectedUnit(clickedUnit);
            return;
        }

        selectedUnit = clickedUnit;
        RefreshSelectionModeForCurrentCommand();

        // Refresh command highlights (attack or move)
        RefreshCommandHighlights();

        Debug.Log($"CombatTest: selected unit '{selectedUnit.name}' at {selectedUnit.position?.gridPos}. Mode={selectionMode}, Command={commandMode}");
    }

    private void HandleClickedEmptyTile(Tile clickedTile)
    {
        if (selectedUnit == null)
            return;

        if (commandMode == CommandMode.Move && selectionMode == SelectionMode.AwaitingMoveTarget)
        {
            if (HexGridManager.Instance != null)
            {
                HashSet<Tile> validMoveTiles = HexGridManager.Instance.GetValidMoveTiles(selectedUnit);
                if (validMoveTiles.Contains(clickedTile))
                {
                    TryMoveSelectedUnitTo(clickedTile);
                    return;
                }
            }

            // Invalid move target -> remove movement command
            ResetToBasicSelectionState();
            return;
        }

        if (commandMode == CommandMode.Attack && selectionMode == SelectionMode.AwaitingAttackTarget)
        {
            // Empty tile can never be a valid attack target
            ResetToBasicSelectionState();
            return;
        }

        Debug.Log("CombatTest: no command selected.");
    }
    private void TrySelectUnit(Tile tile)
    {
        if (!(tile.occupant is UnitController unit))
        {
            Debug.Log("CombatTest: clicked tile has no unit to select.");
            return;
        }

        selectedUnit = unit;
        RefreshSelectionModeForCurrentCommand();
        RefreshCommandHighlights();

        Debug.Log($"CombatTest: selected unit '{selectedUnit.name}' at {selectedUnit.position?.gridPos}. Mode={selectionMode}, Command={commandMode}");
    }

    private void TryMoveSelectedUnitTo(Tile destination)
    {
        if (selectedUnit == null || selectedUnit.position == null)
        {
            Debug.LogWarning("CombatTest: no selected unit or unit has no current tile.");
            return;
        }

        // Checks if destination is valid
        if (destination == null)
        {
            Debug.LogWarning("CombatTest: destination is null.");
            return;
        }

        if (destination == selectedUnit.position)
        {
            Debug.Log("CombatTest: destination is current tile, no movement needed.");
            return;
        }

        if (HexGridManager.Instance == null)
        {
            Debug.LogWarning("CombatTest: HexGridManager.Instance is null.");
            return;
        }

        HashSet<Tile> validMoveTiles = HexGridManager.Instance.GetValidMoveTiles(selectedUnit);
        if (!validMoveTiles.Contains(destination))
        {
            Debug.Log($"CombatTest: clicked tile {destination.gridPos} is not a valid move target.");
            return;
        }

        List<Tile> path = HexPathfinder.FindPath(selectedUnit.position, destination);
        if (path == null || path.Count < 2)
        {
            Debug.LogWarning($"CombatTest: no valid move path for '{selectedUnit.name}' from {selectedUnit.position.gridPos} to {destination.gridPos}.");
            return;
        }

        // Remove blue highlights before movement begins
        HexGridManager.Instance.ClearAllCommandHighlights();

        if (activeMoveRoutine != null)
            StopCoroutine(activeMoveRoutine);

        activeMoveRoutine = StartCoroutine(MoveUnitAlongPath(selectedUnit, path, 0.3f));
    }

    private void TryAttackSelectedUnit(UnitController targetUnit)
    {
        if (selectedUnit == null)
        {
            Debug.LogWarning("CombatTest: no selected unit to perform attack.");
            return;
        }

        if (targetUnit == null)
        {
            Debug.LogWarning("CombatTest: attack target is null.");
            return;
        }

        if (HexGridManager.Instance == null)
        {
            Debug.LogWarning("CombatTest: HexGridManager.Instance is null.");
            return;
        }

        // Get all valid attack tiles
        HashSet<Tile> validAttackTiles = HexGridManager.Instance.GetValidAttackTiles(selectedUnit);
        if (targetUnit.position == null || !validAttackTiles.Contains(targetUnit.position))
        {
            Debug.Log($"CombatTest: attack invalid from '{selectedUnit.name}' to '{targetUnit.name}'.");
            return;
        }

        var target = new CommandTarget(targetUnit.position, targetUnit);
        if (!attackCommand.CanExecute(selectedUnit, target))
        {
            Debug.Log($"CombatTest: attack invalid from '{selectedUnit.name}' to '{targetUnit.name}'.");
            return;
        }

        CommandExecutionRecord result = attackCommand.Execute(selectedUnit, target);
        if (result == null)
        {
            Debug.LogWarning("CombatTest: attack execution returned null.");
            return;
        }

        Debug.Log($"CombatTest: '{selectedUnit.name}' attacked '{targetUnit.name}'.");

        // Keep the unit selected, but clear the active command after the action
        ResetToBasicSelectionState();

        if (UnitConsole.Instance != null && selectedUnit != null)
        {
            UnitConsole.Instance.Initialize(selectedUnit);
        }
    }

    private IEnumerator MoveUnitAlongPath(UnitController unit, List<Tile> path, float secondsPerStep)
    {
        selectionMode = SelectionMode.Moving;
        Debug.Log($"CombatTest: starting movement for '{unit.name}', steps={path.Count - 1}.");

        int length = path.Count;

        for (int i = 1; i < path.Count; i++)
        {
            Tile next = path[i];
            bool moved = unit.MoveToAdjacentTile(next);
            if (!moved)
            {
                Debug.LogWarning($"CombatTest: movement failed at step {i}/{path.Count - 1} toward {next.gridPos}. Unit stopped at {unit.position?.gridPos}.");
                selectionMode = SelectionMode.Idle;
                activeMoveRoutine = null;

                // Re-show valid tiles after failed movement
                RefreshSelectionModeForCurrentCommand();
                RefreshCommandHighlights();
                yield break;
            }

            Debug.Log($"CombatTest: moved step {i}/{path.Count - 1} to {unit.position.gridPos}.");
            yield return new WaitForSeconds(secondsPerStep);
        }

        Debug.Log($"CombatTest: '{unit.name}' arrived at {unit.position.gridPos}. Movement complete.");
        selectionMode = SelectionMode.Idle;
        unit.movesRemaining -= length - 1;
        activeMoveRoutine = null;

        // Keep the unit selected, but clear the active command after the action
        ResetToBasicSelectionState();

        if (UnitConsole.Instance != null && selectedUnit != null)
        {
            UnitConsole.Instance.Initialize(selectedUnit);
        }
    }

    private void OnUnitCommandSelected(UnitCommandSO command)
    {
        if (command == null)
        {
            Debug.LogWarning("CombatTest: received null command from UnitConsole.");
            return;
        }

        if (selectedUnit == null)
        {
            Debug.Log("CombatTest: no selected unit for command.");
            return;
        }

        CommandMode requestedMode = CommandModeFromCategory(command.category);

        // Toggle off if same command clicked again
        if (requestedMode == commandMode)
        {
            ResetToBasicSelectionState();
            return;
        }

        commandMode = requestedMode;
        RefreshSelectionModeForCurrentCommand();
        RefreshCommandHighlights();

        Debug.Log($"CombatTest: command selected '{command.category}'. Mode={selectionMode}, Command={commandMode}.");
    }

    private CommandMode CommandModeFromCategory(CommandCategory category)
    {
        switch (category)
        {
            case CommandCategory.Move:
                return CommandMode.Move;
            case CommandCategory.Attack:
                return CommandMode.Attack;
            default:
                return CommandMode.None;
        }
    }

    private void RefreshSelectionModeForCurrentCommand()
    {
        if (selectionMode == SelectionMode.Moving)
            return;

        if (selectedUnit == null)
        {
            selectionMode = SelectionMode.Idle;
            return;
        }

        selectionMode = SelectionMode.UnitSelected;

        if (commandMode == CommandMode.Move)
        {
            selectionMode = SelectionMode.AwaitingMoveTarget;
        }
        else if (commandMode == CommandMode.Attack)
        {
            selectionMode = SelectionMode.AwaitingAttackTarget;
        }
    }

    private void RefreshCommandHighlights()
    {
        if (HexGridManager.Instance == null)
            return;

        // Removes existing command highlights
        HexGridManager.Instance.ClearAllCommandHighlights();

        // If moving, don't show attack highlights
        if (selectionMode == SelectionMode.Moving)
            return;

        // Returns if no unit is selected or command is active
        if (selectedUnit == null || commandMode == CommandMode.None)
            return;

        // Find matching command on selected unit
        UnitCommandSO matchingCommand = null;
        foreach (UnitCommandSO command in selectedUnit.commands)
        {
            if (command == null)
                continue;

            // Check if command matches current mode
            if (CommandModeFromCategory(command.category) == commandMode)
            {
                matchingCommand = command;
                break;
            }
        }

        // Stop if no matching command found
        if (matchingCommand == null)
            return;

        // ask for all valid tiles for this unit + command combination
        HashSet<Tile> validTiles = HexGridManager.Instance.GetValidTilesForCommand(selectedUnit, matchingCommand);

        // Show blue command highlight on every valid tile
        HexGridManager.Instance.ShowCommandHighlights(validTiles);
    }

    private void CancelSelectionMode()
    {
        if (activeMoveRoutine != null)
        {
            StopCoroutine(activeMoveRoutine);
            activeMoveRoutine = null;
            Debug.Log("CombatTest: active movement coroutine stopped by ESC.");
        }

        selectedUnit = null;
        selectionMode = SelectionMode.Idle;
        commandMode = CommandMode.None;

        if (GridEventHandler.Instance != null)
        {
            GridEventHandler.Instance.ClearSelectedTile();
        }

        // Clears command highlights
        if (HexGridManager.Instance != null)
        {
            HexGridManager.Instance.ClearAllCommandHighlights();
        }

        Debug.Log("CombatTest: selection/command mode cancelled (ESC). State reset to Idle.");
    }

    private void HandleGameStateChanged()
    {
        // Clear highlight if unit moving
        if (selectionMode == SelectionMode.Moving)
        {
            if (HexGridManager.Instance != null)
                HexGridManager.Instance.ClearAllCommandHighlights();
            return;
        }

        // Clears blue highlight if no selected unit
        if (selectedUnit == null)
        {
            if (HexGridManager.Instance != null)
                HexGridManager.Instance.ClearAllCommandHighlights();
            return;
        }

        // Clears command on enemy turn
        if ((Player)selectedUnit.teamID != GameManager.Instance.TurnPlayer)
        {
            // resets command on enemy turn
            commandMode = CommandMode.None;
            selectionMode = SelectionMode.UnitSelected;

            // Clear blue highlight
            if (HexGridManager.Instance != null)
                HexGridManager.Instance.ClearAllCommandHighlights();

            return;
        }

        RefreshSelectionModeForCurrentCommand();
        RefreshCommandHighlights();
    }

    private void ResetToBasicSelectionState()
    {
        // Resets the command mode
        commandMode = CommandMode.None;

        if (selectedUnit == null)
            selectionMode = SelectionMode.Idle;
        else
            // If a unit is still selected
            selectionMode = SelectionMode.UnitSelected;

        if (HexGridManager.Instance != null)
            HexGridManager.Instance.ClearAllCommandHighlights();
    }
}
