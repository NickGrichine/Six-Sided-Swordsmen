using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DataManager : Singleton <DataManager> {
    //No read-only fields --> instead patter to mimic read-only: private + public getter
    private SaveSlot activeSlot;
    private SaveSlot[] slots = new SaveSlot[3]; //todo Assign slots 

    public void Load(SaveSlot data) {
        //get game from Unity Engine and read data before saving to object
        
    }
    public void Save() {
        //create new file and write game data to json file
    }
    public SaveSlot[] GetSaveSlots() {
        return slots; //todo
    }
    public void DeleteActiveGame() {
        //Search active slot
        for(int i = 0; i < 3; i++) {
            if(slots[i] != null && slots[i].Equals(activeSlot)){
                //delete slot file
                if(slots[i].Path != null && File.Exists(slots[i].Path)){
                    File.Delete(slots[i].Path);
                    Debug.Log("Deleted game successfully");
                }
                else{
                    Debug.Log("File not found.");
                }
            }
        }
    }
}