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
    }
}