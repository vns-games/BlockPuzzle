using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIVibrationToggle : MonoBehaviour
{
    [SerializeField] private UIToggleSprite sprite;

    private void OnEnable()
    {
        sprite.OnValueChanged(PlayerPrefs.GetInt("vibration_on", 1) == 1);
    }

    public void ToggleVibration(bool b)
    {
        PlayerPrefs.SetInt("vibration_on", b ? 1 : 0);
        VibrationManager.IsEnabled = b;
    }
}