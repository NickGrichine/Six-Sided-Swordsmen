using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[System.Serializable]
public class DataManager : Singleton <DataManager> {
    //No read-only fields --> instead patter to mimic read-only: private + public getter

    public GameObject popupPanel; //UI Panel
    public Text popupText; //For error or confirmation messages
    
    private SaveSlot activeSlot;
    private SaveSlot[] slots = new SaveSlot[3]; //todo Assign slots 

    public void Load(int gameId) {

        // Validate slot index
        if (gameId < 0 || gameId >= slots.Length)
        {
            StartCoroutine(ReturnToMenuAfterDelay("Invalid game slot. Back to menu!", 2f));
            return;
        }

        // Builds the save-file path
        string path = Path.Combine(Application.persistentDataPath, "Game" + gameId + ".json");

        // Checks if the file does exist
        if (!File.Exists(path))
        {
            StartCoroutine(ReturnToMenuAfterDelay("The game you're looking for does not exist! Back to menu!", 2f));
            return;
        }

        // Read JSON and deserialize into SaveData object
        string json = File.ReadAllText(path);
        SaveData obj = JsonUtility.FromJson<SaveData>(json);

        if (obj == null)
        {
            StartCoroutine(ReturnToMenuAfterDelay("The game you're looking for does not exist! Back to menu!", 2f));
            return;
        }

        // Rebuilds active slot from loaded file
        activeSlot = new SaveSlot(obj, path); // Set active slot to the loaded game
        slots[gameId] = activeSlot; // Store the loaded game in the corresponding slot

        // Reconstructs grid from saved data
        GridAdapter.FromData(HexGridManager.Instance, obj.GetGridData());

        Debug.Log("Loaded game from: " + path);

    }
    //Create Save Data as an argument
    //new SaveData("Game " + gameId, gameId, grid)
    public void Save(HexGridManager grid, int gameId) { //*CALLED AT END OF GAME
        //1. Grid adapter copies grid contents into GridData (done in on grid data creation --> constructor)
        SaveData data = new SaveData("game" + gameId, gameId, grid);    //The constructor calls adapter functions

        //2. create new file and write game data to json file thereby extracting file path
        string json = JsonUtility.ToJson(data, true); //Data record for save data
        string path = Path.Combine(Application.persistentDataPath, "Game" + gameId + ".json");
        //By now Game is saved in data persistence file (json)

        //3. Handle slot Container --> possible need for UI
        activeSlot = new SaveSlot(data, path);
        //3.1 Decide where to slot the SaveData wrapper object
        //3.1.1 User chooses which slot --> need for UI

        //-----------------------------------------

        // if(activeSlot == null) //New Game
        // {
        //     slots[gameId] = new SaveSlot(new SaveData("Game" + gameId, gameId, grid), path);
        //     activeSlot = slots[gameId];
        // }
        // else
        // {
        //     //activeSlot.Data = data;
        // }
        

        // //Assumes there exists some vacant slot --> slot container is not full
        // bool slotFilled = false;
        // for(int i = 0; i < 3; i++)
        // {
        //     if(slots[i] != null)
        //     {
        //         slots[i] = activeSlot;
        //         slotFilled = true;
        //         break;
        //     }
        // }
        // if (!slotFilled)
        // {
        //     //Throw error msg
        //     //No available slots --> Cannot save game

        //     // Show message and return to menu
        //     StartCoroutine(ReturnToMenuAfterDelay("No available slots! Returning to menu.", 2f));
        // }
        //-----------------------------------------

        //4. Write to json contents to object file

        File.WriteAllText(path, json);

        Debug.Log("Saved JSON to: " + path);
    }

    //Helper function for slot container
    IEnumerator ReturnToMenuAfterDelay(string message, float delay)
    {
    // Display the popup
        // popupText.text = message;
        // popupPanel.SetActive(true);

        // Wait for delay
        yield return new WaitForSeconds(delay);

        // Hide popup and return to menu
        //popupPanel.SetActive(false);
        //SceneManager.LoadScene("MainMenu");
    }

    //--------------DEMO 1 -------------

    public DummyGame dummyLoad()
    {
        string path = Path.Combine(Application.persistentDataPath, "DummyGame.json");
        string json = File.ReadAllText(path);
        print("Json Dummy Content" + json);

        return JsonUtility.FromJson<DummyGame>(json);
    }
    //Need to create empty object
    //obj must be serialized
    public void dummySave(System.Object obj)
    {
        string json = JsonUtility.ToJson(obj, true);
        string path = Path.Combine(Application.persistentDataPath, "DummyGame.json");
        //string path = Path.Combine("..", "DummyGame.json");
        path = Path.GetFullPath(path);
        File.WriteAllText(path, json);

        Debug.Log("Dummy Game Saved");
    }

    //------------END of DEMO 1 ---------------

    public SaveSlot[] GetSaveSlots() {
        return slots; 
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