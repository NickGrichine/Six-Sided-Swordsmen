using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// NOTE: dummy class.
public class Tester_UnitConsole : MonoBehaviour
{
    [SerializeField] private UnitConsole unitConsole;
    [SerializeField] private Sprite icon;
    [SerializeField] private string textDescription;

    private ButtonDisplayable displayedObject;

    void Start()
    {
        displayedObject = ScriptableObject.CreateInstance<ButtonDisplayable>();
        displayedObject.SetIcon(icon);
        displayedObject.SetTextDesc(textDescription);
        foreach (CustomButton cmd in unitConsole.commandButtons)
        {
            cmd.onClick += TestDrawButton;
        }

        // TEST:
        TestUnitConsole();
    }

    private void TestUnitConsole()
    {
        unitConsole.SetAttackStat(10);
        unitConsole.SetHealthStat(10, 15);
        unitConsole.SetUnitDescription("This is the unit description.");
        unitConsole.SetDisplayedUnitIcon(displayedObject);
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
        cbutton.onClick += TestDrawButton;
    }


}

