using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// NOTE: dummy class.
public class ButtonTester : MonoBehaviour
{
    [SerializeField] private CustomButton buttonToActivate;
    [SerializeField] private Sprite icon;
    [SerializeField] private string textDescription; // on hover.

    private ButtonDisplayable displayedObject;
    private CustomButton script;

    void Start()
    {
        displayedObject = ScriptableObject.CreateInstance<ButtonDisplayable>();
        displayedObject.SetIcon(icon);
        displayedObject.SetTextDesc(textDescription);

        // Get script.
        // script = buttonToActivate.GetComponent<CustomButton>();
        script = buttonToActivate;

        // Set action.
        script.onClick += DrawButton;
    }

    private void DrawButton(Button button)
    {
        // Resize button.
        RectTransform rect = buttonToActivate.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50f, 50f);

        // Initialize button.
        script.Initialize(displayedObject);
    }



}

