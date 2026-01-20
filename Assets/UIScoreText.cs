using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIScoreText : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        ScoreManager.OnScoreChanged += OnScoreChanged;
    }
    private void OnScoreChanged(int obj)
    {
        _text.text = "SCORE: " + obj;
    }
    private void OnDisable()
    {
        ScoreManager.OnScoreChanged -= OnScoreChanged;
    }
}