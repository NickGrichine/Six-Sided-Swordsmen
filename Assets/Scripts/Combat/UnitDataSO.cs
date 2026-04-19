using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Data")]
public class UnitDataSO : ScriptableObject
{
    public Sprite icon;
    public int maxHealth;
    public int attackStr;
    public int maxMovesPerTurn;
    public int attackRange;
    public int visionRange = 2;
    public int cost = 10;


    // Unit bonus damage info
    // ie. defines how much bonus damage to give when a specific unit type attacks another unit type

    // values are to be set in Unity project
    public enum UnitType { Archer, Knight, Spearman, Swordsman }
    public UnitType unitType;

    // Unit counters (damage matching)
    [System.Serializable]
    public class DamageBonus
    {
        public UnitType targetType;
        public int bonusDamage;
    }
    [SerializeField] public List<DamageBonus> damageBonuses = new List<DamageBonus>();
}
