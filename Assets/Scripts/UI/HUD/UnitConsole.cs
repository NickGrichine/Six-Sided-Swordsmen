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
    [SerializeField] private TextMeshProUGUI healthStat;
    [SerializeField] private TextMeshProUGUI attackStat;
    [SerializeField] private TextMeshProUGUI unitDescription;

    void Awake()
    {
        ClearCommandButtons();
    }

    /// -----------------------
    /// Command Button methods:

    public void ClearCommandButtons()
    {
        foreach (CustomButton cmd in commandButtons)
        {
            if (!cmd) continue;
            cmd.ClearActions();
            cmd.ClearIcon();
        }
    }

    private int _counter = 0;
    public bool SetCommandButton(
            Action<Button> onClick,
            Action<Button> onHover,
            ButtonDisplayable displayedObject)
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

    public void SetDisplayedUnitIcon(ButtonDisplayable displayedObject)
    {
        unitIcon.Initialize(displayedObject);
    }

    public void SetHealthStat(int currentHP, int maxHP)
    {
        healthStat.text = "HP: " + currentHP + "/" + maxHP;
    }

    public void SetAttackStat(int attack)
    {
        attackStat.text = "ATK: " + attack;
    }

    public void SetUnitDescription(string desc)
    {
        unitDescription.text = desc;
    }


}
