using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IButtonDisplayable
{
    Sprite Icon { get; set; }
    string TextDescription { get; set; } // text on hover.
}

