public class DataManager : Singleton <DataManager> {
    private SaveSlot activeSlot;
    private SaveSlot[] slots = new SaveSlot[3]; //todo Assign slots 

    public void Load(SaveSlot data) {
    }
    public void Save() {
    }
    public SaveSlot[] GetSaveSlots() {
        return slots; //todo
    }
    public void DeleteActiveGame() {
        //Search active slot
        for(int i = 0; i < 3; i++) {
            if(slots[i] != null && slots[i].Equals(activeSlot)){
                slots[i] = null;
            }
        }
    }
}