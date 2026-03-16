using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassTurnButton : CustomButton
{
    void Start()
    {
        if (GameManager.Instance)
        {
            onClick += EndTurn;
        }

        Text.text = "Pass Turn";
    }

    private void EndTurn(Button button)
    {
        GameManager.Instance.EndTurn();
    }
}
