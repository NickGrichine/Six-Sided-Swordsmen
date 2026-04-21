using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatExecutor : MonoBehaviour
{
    private readonly MoveExecutor moveExecutor = new MoveExecutor();
    private readonly AttackExecutor attackExecutor = new AttackExecutor();
    private UnitController selectedUnit;
    private Coroutine activeMoveRoutine;
    private enum SelectionMode
     {
         Idle,
         UnitSelected,
         AwaitingMoveTarget,
         AwaitingAttackTarget,
          Moving,
     }
    private enum CommandMode
    {
        None,
        Move,
        Attack,
    }
    private SelectionMode selectionMode = SelectionMode.Idle;
    private CommandMode commandMode = CommandMode.None;



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

        // Reset selected command when clicking new tile. Not sure why this is here, it creates a bug where it deselects everytime we click a tile.
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
            TryMoveSelectedUnitTo(clickedTile); //delegate all responsibility for validating move target to TryMoveSelectedUnitTo which calls the MoveExecutor, which will handle command cancellation if invalid
            
            // Else could be invalid move target -> remove movement command
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
        // Remove blue highlights before movement begins
        HexGridManager.Instance.ClearAllCommandHighlights();
    
        if (!moveExecutor.TryBuildMovePath(selectedUnit, destination, out List<Tile> path))
        return;

        if (HexGridManager.Instance != null)
            HexGridManager.Instance.ClearAllCommandHighlights();

        if (activeMoveRoutine != null)
            StopCoroutine(activeMoveRoutine);

        selectionMode = SelectionMode.Moving;
        activeMoveRoutine = StartCoroutine(
            moveExecutor.ExecuteMove(
                selectedUnit,
                path,
                0.3f,
                OnMoveSucceeded,
                OnMoveFailed));
    }
    private void OnMoveSucceeded(UnitController unit, int stepsMoved)
    {
        selectionMode = SelectionMode.Idle;
        unit.movesRemaining -= stepsMoved;
        activeMoveRoutine = null;
        ResetToBasicSelectionState();
    }
    private void OnMoveFailed(UnitController unit)
    {
        selectionMode = SelectionMode.Idle;
        activeMoveRoutine = null;
        RefreshSelectionModeForCurrentCommand();
        RefreshCommandHighlights();
    }
    private void TryAttackSelectedUnit(UnitController targetUnit)
    {
        if (!attackExecutor.TryExecuteAttack(selectedUnit, targetUnit, out CommandExecutionRecord result))
            return;

        ResetToBasicSelectionState();
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
            RefreshSelectedUnitConsole();

        if (HexGridManager.Instance != null)
            HexGridManager.Instance.ClearAllCommandHighlights();
    }
    private void RefreshSelectedUnitConsole()
    {
        if (UnitConsole.Instance != null && selectedUnit != null)
            UnitConsole.Instance.Initialize(selectedUnit);
    }
}
