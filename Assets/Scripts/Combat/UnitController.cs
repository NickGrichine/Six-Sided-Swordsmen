using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    public int UnitID;
    public int teamID;
    public Tile position;
    public UnitDataSO refData;
    public HealthManager healthManager;
    public List<UnitCommandSO> commands = new List<UnitCommandSO>();
    public int movesRemaining;

    private void Awake()
    {
        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();
    }

    public void StartTurn()
    {
        movesRemaining = refData.maxMovesPerTurn;
    }

    public void ConsumeMoves(int cost)
    {
        movesRemaining -= cost;
        if (movesRemaining < 0) movesRemaining = 0;
    }

    public void OnDeath()
    {
        //TODO:  will have be expanded later. TBA to Kinson?
        Destroy(gameObject);
    }
}