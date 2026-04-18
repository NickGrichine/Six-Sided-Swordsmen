using System.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataPersistence1 : MonoBehaviour
{

    public TMP_InputField inputField;

    public DummyGame game = new DummyGame();

    // Loading from the json file
    void Start()
    {
        //DummyGame game = new DummyGame();
        //DataManager.Instance.dummySave(game);

        //string text = DataManager.Instance.dummyLoad();

        string text = DataManager.Instance.dummyLoad().name;

        Debug.Log("Test Data Persistence Executed!");


        inputField.text = text;
    }


    // Saving to the json file
    void OnApplicationQuit()
    {
        string inputText = inputField.text;

        //DummyGame game = new DummyGame();

        game.name = inputText;
        
        DataManager.Instance.dummySave(game);
    }
}