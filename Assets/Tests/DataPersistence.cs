using System.Data;
using UnityEngine;
public class DataPersistence : MonoBehaviour
{
    void Start()
    {
        DummyGame game = new DummyGame();
        DataManager.Instance.dummySave(game);

        DataManager.Instance.dummyLoad();

        Debug.Log("Test Data Persistence Executed!");
    }
}