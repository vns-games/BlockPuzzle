using System;
using TMPro;
using UnityEngine;

public class UIBestScore : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        ScoreManager.OnBestScoreChanged += OnBestScoreChanged;
        _text.text = "BEST: " + PlayerPrefs.GetInt("BestScore", 0);
    }
    private void OnBestScoreChanged(int obj)
    {
        _text.text = "BEST: " + obj;
    }

    private void OnDisable()
    {
        ScoreManager.OnBestScoreChanged -= OnBestScoreChanged;
    }
}