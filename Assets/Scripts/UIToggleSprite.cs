using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIToggleSprite : MonoBehaviour
{
    private Image _spriteRenderer;
    private void Awake()
    {
        _spriteRenderer = GetComponent<Image>();
    }

    public void OnValueChanged(bool b)
    {
        _spriteRenderer.sprite = GameAssets.Atlas.GetSprite(b ? "on" : "off");
    }
}