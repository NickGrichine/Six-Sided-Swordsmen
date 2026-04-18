using System;

[Serializable]
public class TileOccupantData
{
    public string unitId;
    public string unitName;
    public UnitDataSO.UnitType unitType;
    public int ownerId;
    public int health;
    public int maxHealth;
    public int movesRemaining;
    public int attackRange;
    public int attackStrength;
    public string refDataName;
}
