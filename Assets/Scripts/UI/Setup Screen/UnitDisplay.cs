using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitDisplay : Singleton<UnitDisplay>
{
    [SerializeField] private CustomButton unitIcon;

    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI healthStat;
    [SerializeField] private TextMeshProUGUI attackStat;
    [SerializeField] private TextMeshProUGUI rangeStat;
    [SerializeField] private TextMeshProUGUI movesStat;
    [SerializeField] private TextMeshProUGUI unitDescription;

    [SerializeField] private CanvasGroup unitStatsGroup;


    void Start()
    {
        ClearUnitDisplay();
    }

    public void UpdateUnitDisplay(UnitDataSO unitData)
    {
        if (unitData == null) return;
        Initialize(unitData);
    }

    public void Initialize(UnitDataSO unitData)
    {
        SetCanvasGroupState(unitStatsGroup, true);

        // Display unit stats:
        int maxHP = unitData.maxHealth;
        int maxMoves = unitData.maxMovesPerTurn;
        int attackStrength = unitData.attackStr;
        int attackRange = unitData.attackRange;
        SetHealthStat(maxHP);
        SetAttackStat(attackStrength);
        SetMovesStat(maxMoves);
        SetUnitName(unitData.name);
        SetRangeStat(attackRange);

        // Set unit icon:
        unitIcon.Initialize(unitData.icon);
    }

    public void ClearUnitDisplay()
    {
        HideUnitIcon();
        SetCanvasGroupState(unitStatsGroup, false);
        SetUnitName("");
        SetUnitDescription("");
    }

    /// ------------------
    /// Unit Info methods:

    private void SetDisplayedUnitIcon(IButtonDisplayable displayedObject) { unitIcon.Initialize(displayedObject); }
    private void SetHealthStat(int maxHP) { healthStat.text = "HP: " + maxHP; }
    private void SetAttackStat(int attack) { attackStat.text = "ATK: " + attack; }
    private void SetUnitDescription(string desc) { unitDescription.text = desc; }
    private void SetMovesStat(int maxMoves) { movesStat.text = "Moves: " + maxMoves; }
    private void SetUnitName(string name) { unitName.text = name.Replace("(Clone)", "").Trim(); }
    private void SetRangeStat(int range) { rangeStat.text = "Range: " + range; }

    private void HideUnitIcon() => unitIcon.SetState(Button.BUTTON_STATE.INACTIVE);
    private void SetCanvasGroupState(CanvasGroup cg, bool mode)
    {
        cg.alpha = mode ? 1 : 0;
        cg.interactable = mode;
    }
}
