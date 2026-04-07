using UnityEngine;

public class UnitCommandSO : ScriptableObject, IButtonDisplayable
{
    public int commandID;
    public CommandCategory category;
    public int moveCost;

    public Sprite Icon { get; set; }
    public string TextDescription { get; set; }
}
