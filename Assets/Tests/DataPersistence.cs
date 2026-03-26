using System.Data;
using UnityEngine;
public class DataPersistence : MonoBehaviour
{
    void Start()
    {
        DummyGame game = new DummyGame();
        DataManager.Instance.DummySave(game);

        DataManager.Instance.DummyLoad();
    }
}