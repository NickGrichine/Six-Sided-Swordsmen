using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitConsole : MonoBehaviour
{
    private const int _command_size = 5;

    public CustomButton[] commandButtons = new CustomButton[_command_size];
    [SerializeField] private CustomButton unitIcon;
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI healthStat;
    [SerializeField] private TextMeshProUGUI attackStat;
    [SerializeField] private TextMeshProUGUI rangeStat;
    [SerializeField] private TextMeshProUGUI maxMovesStat;
    [SerializeField] private TextMeshProUGUI unitDescription;

    void Awake()
    {
        ClearCommandButtons();
    }

    public void Initialize(UnitController unitController)
    {
        ClearCommandButtons();

        // Display unit stats:
        int currentHP = unitController.healthManager.GetHealth();
        int maxHP = unitController.refData.maxHealth;
        int maxMoves = unitController.refData.maxMovesPerTurn;
        int attackStr = unitController.refData.attackStr;
        int attackRange = unitController.refData.attackRange;
        SetHealthStat(currentHP, maxHP);
        SetAttackStat(attackStr);
        SetMaxMovesStat(maxMoves);
        SetUnitName(unitController.refData.name);
        SetRangeStat(attackRange);

        // Set command buttons:
        foreach (UnitCommandSO cmd in unitController.commands)
        {
            // TODO: add onClick/onHover Actions:
            SetCommandButton(null, null, cmd);
        }

        // TODO: Set unit icon:
    }

    /// -----------------------
    /// Command Button methods:

    private void ClearCommandButtons()
    {
        foreach (CustomButton cmd in commandButtons)
        {
            if (!cmd) continue;
            cmd.ClearActions();
            cmd.ClearIcon();
        }
    }

    private int _counter = 0;
    private bool SetCommandButton(
            Action<Button> onClick,
            Action<Button> onHover,
            IButtonDisplayable displayedObject)
    {
        if (!commandButtons[_counter]) return false;

        // removing then adding ensures that a specific action is not added multiple times.
        // nothing happens if it doesn't exist.
        commandButtons[_counter].onClick -= onClick;
        commandButtons[_counter].onClick -= onHover;

        // Initialize custom button:
        commandButtons[_counter].onClick += onClick;
        commandButtons[_counter].onClick += onHover;
        commandButtons[_counter].Initialize(displayedObject);

        _counter = (_counter + 1) % _command_size;
        return true;
    }

    /// ------------------
    /// Unit Info methods:

    private void SetDisplayedUnitIcon(IButtonDisplayable displayedObject) { unitIcon.Initialize(displayedObject); }
    private void SetHealthStat(int currentHP, int maxHP) { healthStat.text = "HP: " + currentHP + "/" + maxHP; }
    private void SetAttackStat(int attack) { attackStat.text = "ATK: " + attack; }
    private void SetUnitDescription(string desc) { unitDescription.text = desc; }
    private void SetMaxMovesStat(int maxMoves) { maxMovesStat.text = "MaxMoves: " + maxMoves; }
    private void SetUnitName(string name) { unitName.text = name.Replace("(Clone)", "").Trim(); }
    private void SetRangeStat(int range) { rangeStat.text = "Range: " + range; }


}
