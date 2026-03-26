using System;
using UnityEngine;
[System.Serializable]
public class SaveData {
    [SerializeField]
    private string name; //todo
    private int id;

    GridData gridData;
    //TileData tileData;
    //Constructor
    public SaveData(string name, int id, HexGridManager grid) {
        this.name = name;
        this.id = id;
        
        this.gridData = GridAdapter.ToData(grid);
    }

    public string getName(){
        return name;
    }
    public int GetId()
    {
        return id;
    }
    
    // public bool IsEmpty(SaveData data) {
    //     return false; //todo
    // }
}
