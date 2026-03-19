using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitConsole : Singleton<UnitConsole>
{
    private const int _command_size = 5;

    public CustomButton[] commandButtons = new CustomButton[_command_size];
    [SerializeField] private CustomButton unitIcon;

    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI healthStat;
    [SerializeField] private TextMeshProUGUI attackStat;
    [SerializeField] private TextMeshProUGUI rangeStat;
    [SerializeField] private TextMeshProUGUI movesStat;
    [SerializeField] private TextMeshProUGUI unitDescription;

    [SerializeField] private CanvasGroup unitStatsGroup;
    [SerializeField] private CanvasGroup commandArrayGroup;


    void Start()
    {
        // ClearCommandButtons();
        ClearUnitConsole();
        if (GridEventHandler.Instance)
            GridEventHandler.Instance.onTileClicked += UpdateUnitConsole;
    }

    public void UpdateUnitConsole(Tile tile)
    {
        if (tile == Tile.NullTile) return;

        IOccupant occupant = tile.occupant;
        if (occupant == null)
        {
            ClearUnitConsole();
            return;
        }

        if (!(occupant is UnitController))
        {
            throw new InvalidOperationException("UnitConsole.cs: 'occupant' is not of type UnitController.");
        }

        // TODO: implement differentiation between ally and enemy units.

        UnitController uc = (UnitController)occupant;
        Initialize(uc);
    }

    public void Initialize(UnitController unitController)
    {
        ClearCommandButtons();
        EnableCanvasGroup(unitStatsGroup);
        EnableCanvasGroup(commandArrayGroup);

        // Display unit stats:
        int currentHP = unitController.healthManager.GetHealth();
        int maxHP = unitController.refData.maxHealth;
        int maxMoves = unitController.refData.maxMovesPerTurn;
        int remainingMoves = unitController.movesRemaining;
        int attackStr = unitController.refData.attackStr;
        int attackRange = unitController.refData.attackRange;
        SetHealthStat(currentHP, maxHP);
        SetAttackStat(attackStr);
        SetMovesStat(remainingMoves, maxMoves);
        SetUnitName(unitController.refData.name);
        SetRangeStat(attackRange);

        // Set command buttons:
        foreach (UnitCommandSO cmd in unitController.commands)
        {
            // TODO: add onClick/onHover Actions:
            SetCommandButton(null, null, cmd);
        }

        // TODO: Set unit icon:
        // unitIcon.Initialize( [sprite here] );
    }

    private void ClearUnitConsole()
    {
        HideUnitIcon();
        ClearCommandButtons();
        DisableCanvasGroup(unitStatsGroup);
        DisableCanvasGroup(commandArrayGroup);
        SetUnitName("");
        SetUnitDescription("");
    }

    /// -----------------------
    /// Command Button methods:

    private int _command_index = 0;
    private bool SetCommandButton(
            Action<Button> onClick,
            Action<Button> onHover,
            IButtonDisplayable displayedObject)
    {
        if (_command_index >= _command_size) return false;
        if (!commandButtons[_command_index]) return false;

        // NOTE: removing then adding an action ensures that a specific action
        // is not added multiple times. nothing happens if it doesn't exist.
        commandButtons[_command_index].onClick -= onClick;
        commandButtons[_command_index].onClick -= onHover;

        // Initialize custom button:
        commandButtons[_command_index].onClick += onClick;
        commandButtons[_command_index].onClick += onHover;
        commandButtons[_command_index].Initialize(displayedObject);

        _command_index = (_command_index + 1) % _command_size;
        return true;
    }

    private void ClearCommandButtons()
    {
        _command_index = 0;
        foreach (CustomButton cmd in commandButtons)
        {
            if (!cmd) continue;
            cmd.ClearActions();
            cmd.ClearIcon();
            cmd.SetState(Button.BUTTON_STATE.INACTIVE);
        }
    }

    /// ------------------
    /// Unit Info methods:

    private void SetDisplayedUnitIcon(IButtonDisplayable displayedObject) { unitIcon.Initialize(displayedObject); }
    private void SetHealthStat(int currentHP, int maxHP) { healthStat.text = "HP: " + currentHP + "/" + maxHP; }
    private void SetAttackStat(int attack) { attackStat.text = "ATK: " + attack; }
    private void SetUnitDescription(string desc) { unitDescription.text = desc; }
    private void SetMovesStat(int remainingMoves, int maxMoves) { movesStat.text = "Moves: " + remainingMoves + "/" + maxMoves; }
    private void SetUnitName(string name) { unitName.text = name.Replace("(Clone)", "").Trim(); }
    private void SetRangeStat(int range) { rangeStat.text = "Range: " + range; }

    private void HideUnitIcon() => unitIcon.SetState(Button.BUTTON_STATE.INACTIVE);
    private void EnableCanvasGroup(CanvasGroup cg)
    {
        cg.alpha = 1;
        cg.interactable = true;
    }
    private void DisableCanvasGroup(CanvasGroup cg)
    {
        cg.alpha = 0;
        cg.interactable = false;
    }

}
