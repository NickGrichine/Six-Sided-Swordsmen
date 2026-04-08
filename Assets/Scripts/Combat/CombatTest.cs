using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTest : MonoBehaviour
{
    public HexGridManager grid;
    public GameObject unitPrefab;

    private UnitController unitA;
    private UnitController unitB;
    private UnitController unitC;
    private UnitController unitD;

    private UnitController unitE;

    private UnitController unitF;

    private UnitController unitG;
    private UnitController unitH;
    private UnitController unitI;


    private UnitSpawner spawner;
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
        Debug.Log("CombatTest.Start called");
        var allUnits = FindObjectsOfType<UnitController>();
        foreach (var u in allUnits) Debug.Log($"Unit: {u.gameObject.name} at {u.transform.position}");

        if (FindObjectsOfType<CombatTest>().Length > 1)
        {
            Debug.LogError("Multiple CombatTest in scene, destroying this one");
            Destroy(gameObject);
            return;
        }

        foreach (var unit in allUnits) //cleanup any possible leftover units from previous test runs in the editor, since they won't be cleaned up by Play mode stop if they were spawned in edit mode.
        {
            Destroy(unit.gameObject);
        }

        Debug.Log("After destroy, units left: " + FindObjectsOfType<UnitController>().Length);

        spawner = GetComponent<UnitSpawner>() ?? gameObject.AddComponent<UnitSpawner>();
        spawner.grid = grid;
        spawner.unitPrefab = unitPrefab;

        spawner.SpawnUnit(Player.PLAYER_1, new Vector2Int(0, 0), UnitSpawner.TagUnitType.Knight);
        spawner.SpawnUnit(Player.PLAYER_1, new Vector2Int(2, 1), UnitSpawner.TagUnitType.Archer);
        unitC = spawner.SpawnUnit(Player.PLAYER_1, new Vector2Int(0, 2), UnitSpawner.TagUnitType.Cleric);
        unitD = spawner.SpawnUnit(Player.PLAYER_1, new Vector2Int(1, 3), UnitSpawner.TagUnitType.Spearman);
        unitE = spawner.SpawnUnit(Player.PLAYER_1, new Vector2Int(0, 4), UnitSpawner.TagUnitType.Knight);
        unitF = spawner.SpawnUnit(Player.PLAYER_2, new Vector2Int(4, 6), UnitSpawner.TagUnitType.Knight);
        unitG = spawner.SpawnUnit(Player.PLAYER_2, new Vector2Int(6, 5), UnitSpawner.TagUnitType.Archer);
        unitH = spawner.SpawnUnit(Player.PLAYER_2, new Vector2Int(6, 8), UnitSpawner.TagUnitType.Cleric);
        unitI = spawner.SpawnUnit(Player.PLAYER_2, new Vector2Int(4, 7), UnitSpawner.TagUnitType.Spearman);


        Debug.Log($"Spawned A at {unitA?.position.gridPos}, B at {unitB?.position.gridPos}");
        Debug.Log("Total units after spawn: " + FindObjectsOfType<UnitController>().Length);

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
        Debug.Log($"CombatTest: selected unit '{selectedUnit.name}' at {selectedUnit.position?.gridPos}. Mode={selectionMode}, Command={commandMode}");
    }

    private void HandleClickedEmptyTile(Tile clickedTile)
    {
        if (selectionMode != SelectionMode.AwaitingMoveTarget || commandMode != CommandMode.Move)
        {
            Debug.Log("CombatTest: select a command and a valid target tile/unit before acting.");
            return;
        }

        if (clickedTile.IsOccupied)
        {
            Debug.Log($"CombatTest: destination {clickedTile.gridPos} is occupied. Move cancelled.");
            return;
        }

        TryMoveSelectedUnitTo(clickedTile);
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

        Debug.Log($"CombatTest: selected unit '{selectedUnit.name}' at {selectedUnit.position?.gridPos}. Mode={selectionMode}, Command={commandMode}");
    }

    private void TryMoveSelectedUnitTo(Tile destination)
    {
        if (selectedUnit == null || selectedUnit.position == null)
        {
            Debug.LogWarning("CombatTest: no selected unit or unit has no current tile.");
            return;
        }

        if (destination == selectedUnit.position)
        {
            Debug.Log("CombatTest: destination is current tile, no movement needed.");
            return;
        }

        List<Tile> path = HexPathfinder.FindPath(selectedUnit.position, destination);
        if(path.Count>selectedUnit.movesRemaining+1) //path includes current tile, so steps = path.Count - 1
        {
            Debug.LogWarning($"CombatTest: destination {destination.gridPos} is too far for '{selectedUnit.name}' to move this turn. Path steps={path.Count - 1}, moves remaining={selectedUnit.movesRemaining}.");
            return;
        }
        if (path == null || path.Count < 2)
        {
            Debug.LogWarning($"CombatTest: no valid move path for '{selectedUnit.name}' from {selectedUnit.position.gridPos} to {destination.gridPos}.");
            return;
        }

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
        commandMode = CommandMode.None;
        RefreshSelectionModeForCurrentCommand();
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
                yield break;
            }

            Debug.Log($"CombatTest: moved step {i}/{path.Count - 1} to {unit.position.gridPos}.");
            yield return new WaitForSeconds(secondsPerStep);
        }

        Debug.Log($"CombatTest: '{unit.name}' arrived at {unit.position.gridPos}. Movement complete.");
        selectionMode = SelectionMode.Idle;
        unit.movesRemaining -= length-1;   
        activeMoveRoutine = null;
    }

    private void OnUnitCommandSelected(UnitCommandSO command)
    {
        if (command == null)
        {
            Debug.LogWarning("CombatTest: received null command from UnitConsole.");
            return;
        }

        CommandMode requestedMode = CommandModeFromCategory(command.category);
        if (requestedMode == commandMode)
            commandMode = CommandMode.None;
        else
            commandMode = requestedMode;

        RefreshSelectionModeForCurrentCommand();
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
        {
            return;
        }

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

        Debug.Log("CombatTest: selection/command mode cancelled (ESC). State reset to Idle.");
    }
}