using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIToggleSprite : MonoBehaviour
{
    private Image _spriteRenderer;
    private Image SpriteRenderer
    {
        get
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<Image>();
            }
            return _spriteRenderer;
        }
    }


    public void OnValueChanged(bool b)
    {
        SpriteRenderer.sprite = GameAssets.Atlas.GetSprite(b ? "on" : "off");
    }
}
