using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TestUnitConsoleWithHealthUpdates : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(OneDamagePerSecondToAllUnits());
    }

    private IEnumerator OneDamagePerSecondToAllUnits()
    {
        for (int i = 0; i < 15; i++)
        {
            HealthManager[] allHealthManagers = UnityEngine.Object.FindObjectsByType<HealthManager>(FindObjectsSortMode.None);
            foreach (HealthManager manager in allHealthManagers)
            {
                manager.TakeDamage(1);
            }
            yield return new WaitForSeconds(1f);
        }
    }
}
