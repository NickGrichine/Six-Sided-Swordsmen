using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Data")]
public class UnitDataSO : ScriptableObject
{
    public int maxHealth;
    public int attackStr;
    public int maxMovesPerTurn;
    public int attackRange;
}