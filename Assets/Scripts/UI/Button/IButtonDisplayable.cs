using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IButtonDisplayable
{
    Sprite GetIcon();
    string GetTextDescription();
}

