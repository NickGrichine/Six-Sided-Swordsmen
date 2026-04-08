using System.Data;
using UnityEngine;
using UnityEngine.UI;
public class DataPersistenceDemo2 : MonoBehaviour
{
    void Start()
    {   
        //Load one game without slot capacity factored in
        //Game ID = 1 since we are operating with one game in this demo
        int gameId = 1;
        DataManager.Instance.Load(gameId);
    }

    void OnApplicationQuit()
    {
        int gameId = 1; //Get from text box --> need for UI
        DataManager.Instance.Save(HexGridManager.Instance, gameId);
    }
}

