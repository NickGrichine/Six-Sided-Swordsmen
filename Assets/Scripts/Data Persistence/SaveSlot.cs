using UnityEngine;

[System.Serializable]
public class SaveSlot {
    [SerializeField]
    private SaveData data; //Created when new slot is created --> need constructor

    private string path; //File path

    public SaveSlot(SaveData data, string path) {
        this.data = data;
        this.path = path;
    }

    public SaveData Data {
        get{
            return data;
        }
        set {
            if(value != null){
                data = value;
            }
            else {
                Debug.LogError("SaveData cannot be empty!");
            }
        }
    }

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
    
  // override object.Equals
    public override bool Equals(object obj)
    {
        if(obj is not SaveSlot cmpdata){
            return false;
        }
        return string.Compare(this.getData().getName(), cmpdata.getData().getName(), true) == 0;
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        // TODO: write implementation of GetHashCode() here
        return getData()?.getName()?.ToLower().GetHashCode() ?? 0;
    }
}
