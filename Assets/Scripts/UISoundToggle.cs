using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISoundToggle : MonoBehaviour
{
    [SerializeField] private UIToggleSprite sprite;
    
    private void OnEnable()
    {
        bool isMuted = PlayerPrefs.GetInt("mute", 0) == 1;
        sprite.OnValueChanged(!isMuted);
    }
    public void ToggleMute(bool b)
    {
        Sound.Mute(!b);
    }
}