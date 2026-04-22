using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitConsole : Singleton<UnitConsole>
{
    public event Action<UnitCommandSO> onCommandSelected;

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
    [SerializeField] private CanvasGroup commandButtonArrayGroup;


    void Start()
    {
        // ClearCommandButtons();
        ClearUnitConsole();
        if (GridEventHandler.Instance)
            GridEventHandler.Instance.onTileClicked += UpdateUnitConsole;
    }

    private void OnDestroy()
    {
        if (GridEventHandler.Instance)
            GridEventHandler.Instance.onTileClicked -= UpdateUnitConsole;
    }

    public void UpdateUnitConsole(Tile tile)
    {
        if (tile == null) return;

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

        Initialize((UnitController)occupant);
    }

    public void Initialize(UnitController unitController)
    {
        ClearCommandButtons();
        SetCanvasGroupState(unitStatsGroup, true);
        SetCanvasGroupState(commandButtonArrayGroup, true);

        // Display unit stats:
        int currentHP = unitController.healthManager.GetHealth();
        int maxHP = unitController.refData.maxHealth;
        int maxMoves = unitController.refData.maxMovesPerTurn;
        int remainingMoves = unitController.movesRemaining;
        int attackStrength = unitController.refData.attackStr;
        int attackRange = unitController.refData.attackRange;
        SetHealthStat(currentHP, maxHP);
        SetAttackStat(attackStrength);
        SetMovesStat(remainingMoves, maxMoves);
        SetUnitName(unitController.refData.name);
        SetRangeStat(attackRange);

        // Set command buttons:
        foreach (UnitCommandSO command in unitController.commands)
        {
            SetCommandButton(OnCommandClicked, null, null, command);
        }

        // Set unit icon:
        unitIcon.Initialize(unitController.refData.icon);
        Color32 color = Colours.GetColor(unitController.teamID);
        unitIcon.ChangeIconColor(color);

        Player current_turn_player = GameManager.Instance.TurnPlayer;
        Player unit_belongs_to = (Player)unitController.teamID;
        if (unit_belongs_to == current_turn_player) SetCanvasGroupState(commandButtonArrayGroup, true);
        else
        {
            SetCanvasGroupState(commandButtonArrayGroup, false);
        }
    }

    private void OnCommandClicked(Button button)
    {
        if (!(button is CustomButton customButton))
            return;

        if (!(customButton.displayedObject is UnitCommandSO unitCommand))
            return;

        onCommandSelected?.Invoke(unitCommand);
    }

    public void ClearUnitConsole()
    {
        HideUnitIcon();
        ClearCommandButtons();
        SetCanvasGroupState(unitStatsGroup, false);
        SetCanvasGroupState(commandButtonArrayGroup, false);
        SetUnitName("");
        SetUnitDescription("");
    }

    /// -----------------------
    /// Command Button methods:

    private int _command_index = 0;
    private bool SetCommandButton(
            Action<Button> onClick,
            Action<Button> onHoverEnter,
            Action<Button> onHoverExit,
            IButtonDisplayable displayedObject)
    {
        if (_command_index >= _command_size) return false;
        if (!commandButtons[_command_index]) return false;

        // NOTE: removing then adding an action ensures that a specific action
        // is not added multiple times. nothing happens if it doesn't exist.
        commandButtons[_command_index].onClick -= onClick;
        commandButtons[_command_index].onHoverEnter -= onHoverEnter;
        commandButtons[_command_index].onHoverExit -= onHoverExit;

        // Initialize custom button:
        commandButtons[_command_index].onClick += onClick;
        commandButtons[_command_index].onHoverEnter += onHoverEnter;
        commandButtons[_command_index].onHoverExit += onHoverExit;
        commandButtons[_command_index].Initialize(displayedObject);

        _command_index = (_command_index + 1) % _command_size;
        return true;
    }

    private void ClearCommandButtons()
    {
        _command_index = 0;
        foreach (CustomButton command in commandButtons)
        {
            if (!command) continue;
            command.ClearActions();
            command.ClearIcon();
            command.SetState(Button.BUTTON_STATE.INACTIVE);
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
    private void SetCanvasGroupState(CanvasGroup cg, bool mode)
    {
        cg.alpha = mode ? 1 : 0;
        cg.interactable = mode;
    }

}
