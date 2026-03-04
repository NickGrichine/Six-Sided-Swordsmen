using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Why ScriptableObject?
// This allows storing an instance of this class like a prefab; makes it easy
// to apply changes to buttons.

[CreateAssetMenu]
public class ButtonDisplayable : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string textDescription; // on hover.

    public void SetIcon(Sprite icon) { this.icon = icon; }
    public void SetTextDesc(string text) { textDescription = text; }
    public Sprite GetIcon() { return icon; }
    public string GetTextDesc() { return textDescription; }


}

