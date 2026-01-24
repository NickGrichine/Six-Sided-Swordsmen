using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    public static T Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            throw new System.Exception($"Multiple instances of singleton {typeof(T).Name} exist in scene. Consider not destroying and instantiating singleton managers.");
        }
        Instance = this as T;
    }
}
