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

    // for now, all values are set to 0, to be defined by Michael?
    public enum UnitType { Archer, Knight, Spearman, Swordsman }
    public UnitType unitType;

    // Unit counters (damage matching)
    [SerializeField]
    public Dictionary<UnitType, int> damageBonusesArcher = new Dictionary<UnitType, int>()
    {
        { UnitType.Archer, 0 },
        { UnitType.Knight, 0 },
        { UnitType.Spearman, 0 },
        { UnitType.Swordsman, 0 }
    };

    [SerializeField]
    public Dictionary<UnitType, int> damageBonusesKnight = new Dictionary<UnitType, int>()
    {
        { UnitType.Archer, 0 },
        { UnitType.Knight, 0 },
        { UnitType.Spearman, 0 },
        { UnitType.Swordsman, 0 }
    };

    [SerializeField]
    public Dictionary<UnitType, int> damageBonusesSpearman = new Dictionary<UnitType, int>()
    {
        { UnitType.Archer, 0 },
        { UnitType.Knight, 0 },
        { UnitType.Spearman, 0 },
        { UnitType.Swordsman, 0 }
    };

    [SerializeField]
    public Dictionary<UnitType, int> damageBonusesSwordsman = new Dictionary<UnitType, int>()
    {
        { UnitType.Archer, 0 },
        { UnitType.Knight, 0 },
        { UnitType.Spearman, 0 },
        { UnitType.Swordsman, 0 }
    };
}
