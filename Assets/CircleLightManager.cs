using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CircleLightManager : MonoBehaviour
{
    [SerializeField] private List<CircleLight> lights;

    [Header("Snake Settings")]
    [SerializeField] private int snakeLength = 5;
    [SerializeField] private float snakeDuration = 1f;

    [Header("Incredible Settings")]
    [SerializeField] private float doubleSnakeDuration = 1f;
    [SerializeField] private Color incredibleColor = Color.cyan;

    [Header("Visual Settings")]
    [SerializeField] private float minAlpha = 0.1f; 
    [SerializeField] private float blinkDuration = 0.25f;

    private Status _status;

    // Tween Referansları
    private Tween snakeTween;
    private Tween breatheTween;
    private Tween blinkTween;

    private float _lastSnakeHeadPosition = 0f;
    private int _remainingMoves;

    // GPU Instancing Değişkenleri
    private MaterialPropertyBlock _propBlock;
    
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int ShapeColorId = Shader.PropertyToID("_Shape2Color");
    
    private Color _currentBaseColor = Color.white; 

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

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

    private void Start()
    {
        _currentBaseColor = Color.white;
        _status = Status.Breathe;
        Breathe();
    }

    private void OnCombo(int remainingMoves)
    {
        _remainingMoves = remainingMoves;
        
        // 1. DURUM: KOMBO YOK -> BEYAZ BREATHE
        if (_remainingMoves <= 0)
        {
            ChangeBaseColor(Color.white); 
            Breathe();
            _status = Status.Breathe;
        }
        // 2. DURUM: KOMBO VAR -> RENKLİ YILAN
        else
        {
            bool isGreenMode = false;

            if (_remainingMoves >= 3)
            {
                ChangeBaseColor(Color.green);
                isGreenMode = true; // Sadece yeşilde blink yapacağız
            }
            else if (_remainingMoves == 2)
            {
                ChangeBaseColor(new Color(1, .8f, 0)); // Sarı
            }
            else
            {
                ChangeBaseColor(Color.red);
            }

            if (isGreenMode)
            {
                // Sadece YEŞİL ise ÇİFT BLINK yap, sonra Snake başlasın
                PlayDoubleBlinkAndStartSnake();
            }
            else
            {
                // Sarı veya Kırmızı ise direkt Snake başlasın
                StartSnakeEffect();
            }

            _status = Status.Snake;
        }
    }

    private void OnIncredible()
    {
        ChangeBaseColor(incredibleColor);
        StartDoubleSnakeEffect();
        _status = Status.DoubleSnake;
    }

    // --- EFEKTLER ---

    private void PlayDoubleBlinkAndStartSnake()
    {
        KillActiveTweens(); 

        // Blink hızını biraz artırdık (Daha seri çaksın)
        float fastBlinkDuration = blinkDuration * 0.7f;

        // Parlaktan Sönüğe doğru blink
        blinkTween = DOVirtual.Float(1f, minAlpha, fastBlinkDuration, (value) => 
        {
            UpdateAllLightsAlpha(value);
        })
        .SetEase(Ease.OutQuad)
        .SetLoops(2, LoopType.Restart) // 2 Kere Çak
        .OnComplete(() => 
        {
            blinkTween = null;
            StartSnakeEffect();
        });
    }

    private void Breathe()
    {
        // Zaten oynuyorsa tekrar başlatma
        if (breatheTween != null && breatheTween.IsActive() && breatheTween.IsPlaying()) return;

        KillActiveTweens();
        
        // 1. Önce ışıkları tamamen "Söndür" (minAlpha seviyesine çek)
        // Böylece efekt başladığında ışıklar sönük olur.
        UpdateAllLightsAlpha(0);

        // 2. Animasyon Yönü Değiştirildi:
        // Eskiden: 1f -> minAlpha (Parlaktan Sönüğe) idi.
        // Şimdi: minAlpha -> 1f (Sönükten Parlaya) yaptık.
        breatheTween = DOVirtual.Float(minAlpha, 1f, 2f, (value) => 
        {
            UpdateAllLightsAlpha(value);
        })
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }

    public void StartSnakeEffect()
    {
        if (snakeTween != null) snakeTween.Kill();
        if (breatheTween != null) breatheTween.Kill();
        
        float startPos = _lastSnakeHeadPosition % lights.Count;
        float endPos = startPos + lights.Count;

        snakeTween = DOVirtual.Float(startPos, endPos, snakeDuration, UpdateSnakeLights)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
    }

    public void StartDoubleSnakeEffect()
    {
        if (_status == Status.DoubleSnake) return;

        KillActiveTweens();

        snakeTween = DOVirtual.Float(0, lights.Count, doubleSnakeDuration, UpdateDoubleSnakeLights)
            .SetLoops(8, LoopType.Restart)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                OnCombo(_remainingMoves);
            });
    }

    // --- IŞIK GÜNCELLEME MANTIKLARI ---

    private void UpdateSnakeLights(float headPosition)
    {
        _lastSnakeHeadPosition = headPosition;
        int headIndex = Mathf.FloorToInt(headPosition) % lights.Count;

        // Reset
        for(int i = 0; i < lights.Count; i++) 
            SetLightColorWithAlpha(lights[i], minAlpha);

        // Yılan
        for(int i = 0; i < snakeLength; i++)
        {
            int targetIndex = (headIndex - i + lights.Count) % lights.Count;
            float alphaRatio = 1f - ((float)i / snakeLength);
            float finalAlpha = Mathf.Max(alphaRatio, minAlpha);
            
            SetLightColorWithAlpha(lights[targetIndex], finalAlpha);
        }
    }

    private void UpdateDoubleSnakeLights(float headPosition)
    {
        int headIndex1 = Mathf.FloorToInt(headPosition) % lights.Count;
        int headIndex2 = (headIndex1 + (lights.Count / 2)) % lights.Count;

        // Reset
        for(int i = 0; i < lights.Count; i++) 
            SetLightColorWithAlpha(lights[i], minAlpha);

        // Çift Yılan
        for(int i = 0; i < snakeLength; i++)
        {
            float alphaRatio = 1f - ((float)i / snakeLength);
            float finalAlpha = Mathf.Max(alphaRatio, minAlpha);

            int t1 = (headIndex1 - i + lights.Count) % lights.Count;
            SetLightColorWithAlpha(lights[t1], finalAlpha);

            int t2 = (headIndex2 - i + lights.Count) % lights.Count;
            SetLightColorWithAlpha(lights[t2], finalAlpha);
        }
    }

    // --- YARDIMCI VE ÇEKİRDEK METOTLAR ---

    void ChangeBaseColor(Color c)
    {
        _currentBaseColor = c;
    }

    private void UpdateAllLightsAlpha(float alpha)
    {
        foreach (var l in lights)
        {
            SetLightColorWithAlpha(l, alpha);
        }
    }

    private void SetLightColorWithAlpha(CircleLight lightObj, float alpha)
    {
        if (lightObj == null || lightObj.SpriteRenderer == null) return;

        Renderer r = lightObj.SpriteRenderer;

        r.GetPropertyBlock(_propBlock);

        Color finalColor = _currentBaseColor;
        finalColor.a = alpha;

        _propBlock.SetColor(GlowColorId, finalColor);
        _propBlock.SetColor(ShapeColorId, finalColor);

        r.SetPropertyBlock(_propBlock);
    }

    private void KillActiveTweens()
    {
        if(breatheTween != null) breatheTween.Kill();
        if(snakeTween != null) snakeTween.Kill();
        if(blinkTween != null) blinkTween.Kill();
    }

    enum Status
    {
        Snake,
        DoubleSnake,
        Breathe
    }
}