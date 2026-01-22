using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIReviveArea : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private UnityEvent onInterAdsShown, onRewardedAdsShown;

    private Tween _tween;
    private void OnEnable()
    {
        StartCountdown();
    }

    void StartCountdown()
    {
        int seconds = 5;

       _tween = DOVirtual.Int(seconds, 0, seconds, value => countdownText.text = value + "").OnComplete(OnTimeIsUp).SetEase(Ease.Linear);
    }
    private void OnTimeIsUp()
    {
        print("Show Inter Ads");
        onInterAdsShown?.Invoke();
    }

    public void OnClickRevive()
    {
        _tween?.Kill();
        print("Show Rewarded Ads");
    }
}