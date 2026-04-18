using System.Data;
using UnityEngine;
using UnityEngine.UI;
public class DataPersistence : MonoBehaviour
{
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool saveOnQuit = true;
    [SerializeField] private int gameId = 1;


    void Start()
    {   
        if (loadOnStart)
        {
            DataManager.Instance.Load(gameId);
        }
    }

    void OnApplicationQuit()
    {
        if (saveOnQuit)
        {
            DataManager.Instance.Save(HexGridManager.Instance, gameId);
        }
    }
}

