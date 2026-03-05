using UnityEngine;
[System.Serializable]
public class SaveData {
    [SerializeField]
    private string name; //todo
    //Constructor
    public SaveData(string name) {
        this.name = name;
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
    public bool IsAvailable(SaveData data) {
        return false; //todo
    }
}