using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISoundToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    private void Awake()
    {
        toggle.onValueChanged.AddListener(ToggleMute);
    }
    private void OnEnable()
    {
        bool isMuted = PlayerPrefs.GetInt("mute", 0) == 1;
        toggle.isOn = !isMuted;
    }
    private void ToggleMute(bool b)
    {
        Sound.ToggleMute();
    }
}