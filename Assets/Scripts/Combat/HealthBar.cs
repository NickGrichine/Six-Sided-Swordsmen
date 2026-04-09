using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    public Slider slider;

    public void Show(bool shouldShow)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = shouldShow ? 1f : 0f;
    }

    public void SetHealth(int health)
    {
        slider.value = health;
    }

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;
    }
}
