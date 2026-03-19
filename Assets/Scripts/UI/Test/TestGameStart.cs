using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// NOTE: Attach this to the GameManager game object to test the start of the game.
public class TestGameStart : MonoBehaviour
{
    void Start()
    {
        IEnumerator delayedStart()
        {
            yield return null;
            GameManager.Instance.StartGame();
        }
        StartCoroutine(delayedStart());
    }
}
