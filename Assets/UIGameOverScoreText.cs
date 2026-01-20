using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIGameOverScoreText : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        _text.text = "SCORE: " + ScoreManager.Instance.CurrentScore;
    }
}
