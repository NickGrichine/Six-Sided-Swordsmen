public class SaveSlot {
    private SaveData data;

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
        //
        // See the full list of guidelines at
        //   http://go.microsoft.com/fwlink/?LinkID=85237
        // and also the guidance for operator== at
        //   http://go.microsoft.com/fwlink/?LinkId=85238
        //

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