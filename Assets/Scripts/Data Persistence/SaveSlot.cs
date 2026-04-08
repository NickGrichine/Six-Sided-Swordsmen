using UnityEngine;

[System.Serializable]
public class SaveSlot {
    [SerializeField] private SaveData data; //Created when new slot is created --> need constructor
    [SerializeField]private string path; //File path

    public SaveSlot(SaveData data, string path) 
    {
        this.data = data;
        this.path = path;
    }

    public SaveData Data {
        get => data;

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
        get => path;
        set{
            if(!string.IsNullOrEmpty(value)){
                path = value;
            }
            else{
                Debug.LogError("Name cannot be empty!");
            }
        }
    }

    public SaveData GetData(){
        return data;
    }
    
  // override object.Equals
    public override bool Equals(object obj)
    {
        if (obj is not SaveSlot other)
            return false;

        if (data == null || other.data == null)
            return false;

        return string.Compare(data.GetName(), other.data.GetName(), true) == 0;
    }
    
    // override object.GetHashCode
    public override int GetHashCode()
    {
        // TODO: write implementation of GetHashCode() here
        return data?.GetName()?.ToLower().GetHashCode() ?? 0;
    }
}
