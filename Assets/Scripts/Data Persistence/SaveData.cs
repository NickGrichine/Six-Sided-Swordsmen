using UnityEngine;
[System.Serializable]
public class SaveData {
    [SerializeField]
    private string name; //todo
    private int id;
    //Constructor
    public SaveData(string name, int id) {
        this.name = name;
        this.id = id;
    }

    public string getName(){
        return name;
    }

    public string GetString() {
        return ""; //todo
    }
    public bool IsEmpty(SaveData data) {
        return false; //todo
    }
}
