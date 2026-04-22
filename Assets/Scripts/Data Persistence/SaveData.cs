using System;
using UnityEngine;
[System.Serializable]
public class SaveData {

    [SerializeField] private string name;
    [SerializeField] private int id;
    [SerializeField] private GridData gridData;

    [SerializeField] private int turnNumber;
    [SerializeField] private Player turnPlayer;

    public SaveData()
    {
        this.name = "";
        this.id = -1;
        this.gridData = null;
        this.turnNumber = 1;
        this.turnPlayer = Player.PLAYER_1;
    }

    public SaveData(string name, int id, HexGridManager grid, int turnNumber, Player turnPlayer) 
    {
        this.name = name;
        this.id = id;
        this.gridData = GridAdapter.ToData(grid);
        this.turnNumber = turnNumber;
        this.turnPlayer = turnPlayer;
    }

    public string GetName()
    {
        return name;
    }
    public int GetId()
    {
        return id;
    }
    
    public GridData GetGridData()
    {
        return gridData;
    }

    public int GetTurnNumber()
    {
        return turnNumber;
    }

    public Player GetTurnPlayer()
    {
        return turnPlayer;
    }
}
