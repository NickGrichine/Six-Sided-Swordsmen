using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewHelpPage", menuName = "UI/HelpWindow")]
public class HelpPageSO : ScriptableObject
{
    public Sprite sprite;
    public string title;
    public string description;
    public HelpPageSO next = null;
}
