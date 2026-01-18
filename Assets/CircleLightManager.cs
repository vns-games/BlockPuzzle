using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CircleLightManager : MonoBehaviour
{
    [SerializeField] private List<CircleLight> lights;

    [Header("Snake Settings")]
    [SerializeField] private int snakeLength;
    [SerializeField] private float snakeDuration;

    [Header("Incredible Settings")]
    [SerializeField] private float doubleSnakeDuration = 1f;
    [SerializeField] private Color incredibleColor = Color.cyan;

    [SerializeField] private float minAlpha;

    private Status _status;

    private Tween snakeTween;
    private Sequence breathe;

    // Yılanın en son kaldığı konumu tutar
    private float _lastSnakeHeadPosition = 0f;
    private int _remainingMoves;

    private void OnEnable()
    {
        ScoreManager.OnCombo += OnCombo;
        ScoreManager.OnIncredible += OnIncredible;
    }
    private void OnDisable()
    {
        ScoreManager.OnCombo -= OnCombo;
        ScoreManager.OnIncredible -= OnIncredible;
    }

    private void OnCombo(int remainingMoves)
    {
        _remainingMoves = remainingMoves;
        // 1. DURUM: KOMBO YOK (0 veya daha az) -> BREATHE
        if (_remainingMoves <= 0)
        {
            ChangeColor(Color.white); // Sakin mod rengi
            Breathe();
            _status = Status.Breathe;
        }
        // 2. DURUM: KOMBO VAR (3, 2, 1) -> HER TÜRLÜ YILAN (SNAKE)
        else
        {
            // Hak sayısına göre rengi belirle
            if (_remainingMoves >= 3)
            {
                ChangeColor(Color.green); // 3 Hak varken Yeşil Yılan
            }
            else if (_remainingMoves == 2)
            {
                ChangeColor(new Color(1, .8f, 0)); // 2 Hak varken Sarı Yılan
            }
            else // _remainingMoves == 1
            {
                ChangeColor(Color.red); // 1 Hak varken Kırmızı Yılan
            }


            // Hepsinde Yılan efekti çalıştır (Kaldığı yerden devam eder)
            StartSnakeEffect();
            _status = Status.Snake;
        }
    }

    private void OnIncredible()
    {
        ChangeColor(incredibleColor);
        StartDoubleSnakeEffect();
        _status = Status.DoubleSnake;
    }

    private void Start()
    {
        // Başlangıçta Kombo yok, Breathe çalışsın
        _status = Status.Breathe;
        Breathe();
    }

    private void Breathe()
    {
        if (breathe != null && breathe.IsActive() && breathe.IsPlaying()) return;

        KillActiveTweens();
        ResetAllLights();

        breathe = DOTween.Sequence();
        foreach (var circleLight in lights)
        {
            breathe.Join(circleLight.SpriteRenderer.DOFade(1f, 1f));
        }
        breathe.SetLoops(-1, LoopType.Yoyo).Play();
    }

    public void StartSnakeEffect()
    {
        // Zaten Yılan modundaysak ve animasyon çalışıyorsa dokunma (Renk değişse bile akış bozulmasın)
        if (_status == Status.Snake) return;

        KillActiveTweens();
        ResetAllLights();

        // Kaldığı yerden devam et
        float startPos = _lastSnakeHeadPosition % lights.Count;
        float endPos = startPos + lights.Count;

        snakeTween = DOVirtual.Float(startPos, endPos, snakeDuration, UpdateLights)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
    }

    public void StartDoubleSnakeEffect()
    {
        if (_status == Status.DoubleSnake) return;

        KillActiveTweens();
        ResetAllLights();

        snakeTween = DOVirtual.Float(0, lights.Count, doubleSnakeDuration, UpdateDoubleSnakeLights)
            .SetLoops(8, LoopType.Restart)
            .SetEase(Ease.Linear).OnComplete(() =>
            {
                OnCombo(_remainingMoves);
            });
    }

    private void UpdateDoubleSnakeLights(float headPosition)
    {
        int headIndex1 = Mathf.FloorToInt(headPosition) % lights.Count;
        int headIndex2 = (headIndex1 + (lights.Count / 2)) % lights.Count;

        for(int i = 0; i < lights.Count; i++) SetLightAlpha(lights[i], minAlpha);

        for(int i = 0; i < snakeLength; i++)
        {
            float alphaRatio = 1f - ((float)i / snakeLength);
            float finalAlpha = Mathf.Max(alphaRatio, minAlpha);

            int t1 = (headIndex1 - i + lights.Count) % lights.Count;
            SetLightAlpha(lights[t1], finalAlpha);

            int t2 = (headIndex2 - i + lights.Count) % lights.Count;
            CircleLight l2 = lights[t2];
            if (l2.SpriteRenderer.color.a < finalAlpha) SetLightAlpha(l2, finalAlpha);
        }
    }

    private void UpdateLights(float headPosition)
    {
        _lastSnakeHeadPosition = headPosition;

        int headIndex = Mathf.FloorToInt(headPosition) % lights.Count;

        for(int i = 0; i < lights.Count; i++) SetLightAlpha(lights[i], minAlpha);

        for(int i = 0; i < snakeLength; i++)
        {
            int targetIndex = (headIndex - i + lights.Count) % lights.Count;
            float alphaRatio = 1f - ((float)i / snakeLength);
            float finalAlpha = Mathf.Max(alphaRatio, minAlpha);
            SetLightAlpha(lights[targetIndex], finalAlpha);
        }
    }

    private void KillActiveTweens()
    {
        breathe?.Kill();
        snakeTween?.Kill();
    }

    void ChangeColor(Color c)
    {
        foreach (var circleLight in lights)
        {
            circleLight.SpriteRenderer.material.SetColor("_GlowColor", c);
        }
    }

    private void SetLightAlpha(CircleLight lightObj, float alpha)
    {
        if (lightObj != null && lightObj.SpriteRenderer != null)
        {
            Color c = lightObj.SpriteRenderer.color;
            c.a = alpha;
            lightObj.SpriteRenderer.color = c;
        }
    }

    private void ResetAllLights()
    {
        foreach (var l in lights) SetLightAlpha(l, minAlpha);
    }

    enum Status
    {
        Snake,
        DoubleSnake,
        Breathe
    }
}