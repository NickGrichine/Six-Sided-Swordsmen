using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// NOTE: dummy class.
public class TestUnitConsole : MonoBehaviour
{
    [SerializeField] private UnitConsole unitConsole;
    [SerializeField] private Sprite icon;
    [SerializeField] private string textDescription;
    [SerializeField] private UnitDataSO unitData;

    private IButtonDisplayable displayedObject;

    void Start()
    {
        displayedObject = ScriptableObject.CreateInstance<UnitCommandSO>();
        displayedObject.Icon = icon;
        displayedObject.TextDescription = textDescription;
        foreach (CustomButton cmd in unitConsole.commandButtons)
        {
            cmd.onClick += TestDrawButton;
        }

        // TEST:
        TestConsole();
        // TestInitialization();
        TestInactiveButtion();
    }

    private void TestInitialization()
    {
        foreach (CustomButton cmd in unitConsole.commandButtons)
        {
            cmd.Initialize(displayedObject);
        }
    }

    private void TestInactiveButtion()
    {
        unitConsole.commandButtons[0].ClearIcon();
        unitConsole.commandButtons[0].SetState(Button.BUTTON_STATE.INACTIVE);
    }

    private void TestConsole()
    {
        UnitController uc = new UnitController();
        uc.refData = Instantiate(unitData);
        uc.healthManager = new HealthManager();
        uc.healthManager.SetMaxHealth(uc.refData.maxHealth);
        unitConsole.Initialize(uc);

        // unitConsole.SetAttackStat(10);
        // unitConsole.SetHealthStat(10, 15);
        // unitConsole.SetUnitDescription("This is the unit description.");
        // unitConsole.SetDisplayedUnitIcon(displayedObject);
    }

    private void TestDrawButton(Button button)
    {
        // Initialize button.
        CustomButton cbutton = button as CustomButton;
        RectTransform rect = cbutton.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(49f, 49f);
        cbutton.Initialize(displayedObject);
        cbutton.ClearActions();
        cbutton.onClick += TestClearButton;
    }

    private void TestClearButton(Button button)
    {
        CustomButton cbutton = button as CustomButton;
        cbutton.ClearActions();
        cbutton.ClearIcon();
        // cbutton.onClick += TestDrawButton;
        cbutton.onClick += TestDisableButton;
    }

    private void TestDisableButton(Button button)
    {
        CustomButton cbutton = button as CustomButton;
        cbutton.ClearActions();
        cbutton.ClearIcon();
        cbutton.SetState(Button.BUTTON_STATE.INACTIVE);
    }


}

