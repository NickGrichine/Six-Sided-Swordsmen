using UnityEngine;

[CreateAssetMenu(fileName = "NewCommand", menuName = "Commands/UnitCommand")]
public class UnitCommandSO : ScriptableObject, IButtonDisplayable
{
    public int commandID;
    public CommandCategory category;
    public int moveCost;

    [SerializeField] private Sprite icon;
    [SerializeField] private string textDescription;

    public Sprite Icon { get => icon; set => icon = value; }
    public string TextDescription { get => textDescription; set => textDescription = value; }
}
