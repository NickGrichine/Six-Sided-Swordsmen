using UnityEngine;

[System.Serializable]
public class SaveSlot {
    [SerializeField]
    private SaveData data;

    private string path; //File path

    public string Path{
        get{
            return path;
        }
        set{
            if(!string.IsNullOrEmpty(value)){
                path = value;
            }
            else{
                Debug.LogError("Name cannot be empty!");
            }
        }
    }

    public SaveData getData(){
        return data;
    }

    public void Write(SaveData data) {
        //todo
    }
    public SaveData Read() {
        return data; //For compilation purposes --> todo subject to change
    }
    public void Clear() {
        //todo
    }
    public string GetMainText() {
        return ""; //To compile --> todo subject to change
    }
  // override object.Equals
    public override bool Equals(object obj)
    {
        if(obj is not SaveSlot cmpdata){
            return false;
        }
        // TODO: write your implementation of Equals() here
        return string.Compare(this.getData().getName(), cmpdata.getData().getName(), true) == 0;
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        // TODO: write your implementation of GetHashCode() here
        return getData()?.getName()?.ToLower().GetHashCode() ?? 0;
    }
}
