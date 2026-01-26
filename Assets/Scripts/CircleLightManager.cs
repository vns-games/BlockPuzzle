using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CircleLightManager : MonoBehaviour
{
    [SerializeField] private List<CircleLight> lights;

    [Header("Alpha Ayarları")]
    [Range(0f, 1f)] public float sr1MaxAlpha = 0.5f; // SR1 (Renkli) en fazla %50 görünsün
    [Range(0f, 1f)] public float sr2MaxAlpha = 1.0f; // SR2 (Beyaz) tamamen görünsün
    
    [Header("Snake Settings")]
    [SerializeField] private int snakeLength = 5;
    [SerializeField] private float snakeDuration = 1f;

    [Header("Incredible Settings")]
    [SerializeField] private float doubleSnakeDuration = 1f;
    [SerializeField] private Color incredibleColor = Color.cyan;

    [Header("Visual Settings")]
    [SerializeField] private float minAlpha = 0f; 
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
    
    // Shader Property ID'leri
    private static readonly int ShapeColorId = Shader.PropertyToID("_Shape2Color");
    private static readonly int MainColorId = Shader.PropertyToID("_Color"); // Standart Shaderlar için
    // Diğer ID'lerin yanına ekle
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    
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
        Breathe();
        _status = Status.Breathe;
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
                isGreenMode = true; 
            }
            else if (_remainingMoves == 2)
            {
                ChangeBaseColor(new Color(1, .6f, 0)); // Sarı
            }
            else
            {
                ChangeBaseColor(Color.red);
            }

            if (isGreenMode)
            {
                PlayDoubleBlinkAndStartSnake();
            }
            else
            {
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

    // --- EFEKTLER (MANTIKLARI AYNI) ---

    private void PlayDoubleBlinkAndStartSnake()
    {
        KillActiveTweens(); 
        float fastBlinkDuration = blinkDuration * 0.7f;

        blinkTween = DOVirtual.Float(1f, minAlpha, fastBlinkDuration, (value) => 
        {
            UpdateAllLightsAlpha(value);
        })
        .SetEase(Ease.OutQuad)
        .SetLoops(2, LoopType.Restart) 
        .OnComplete(() => 
        {
            blinkTween = null;
            StartSnakeEffect();
        });
    }

    private void Breathe()
    {
        if (_status == Status.Breathe) return;

        KillActiveTweens();
        UpdateAllLightsAlpha(0);

        breatheTween = DOVirtual.Float(minAlpha, 1, 2f, (value) => 
        {
            UpdateAllLightsAlpha(value);
        })
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }

    public void StartSnakeEffect()
    {
        snakeTween?.Kill();
        breatheTween?.Kill();

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

    // --- IŞIK GÜNCELLEME (AYNI) ---

    private void UpdateSnakeLights(float headPosition)
    {
        _lastSnakeHeadPosition = headPosition;
        int headIndex = Mathf.FloorToInt(headPosition) % lights.Count;

        for(int i = 0; i < lights.Count; i++) 
            SetLightColorWithAlpha(lights[i], minAlpha);

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

        for(int i = 0; i < lights.Count; i++) 
            SetLightColorWithAlpha(lights[i], minAlpha);

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

    // --- YARDIMCILAR ---

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
    
private void UpdateOuterLight(CircleLight lightObj, float mainAlpha)
    {
        if (lightObj == null || lightObj.SpriteRenderer1 == null) return;

        _propBlock.Clear();
        lightObj.SpriteRenderer1.GetPropertyBlock(_propBlock);

        // RENK: Mevcut oyun rengi (Kırmızı, Yeşil vs.)
        Color color = _currentBaseColor;
        color.a = 1f; // Rengin kendisi tam opak
        _propBlock.SetColor(MainColorId, color);
        // Eğer shader'da başka renk kanalları varsa onları da set et:
        // _propBlock.SetColor(GlowColorId, color);

        // ALPHA: Gelen alpha * 0.5 (veya sr1MaxAlpha)
        float finalAlpha = mainAlpha * sr1MaxAlpha;
        _propBlock.SetFloat(AlphaId, finalAlpha);

        lightObj.SpriteRenderer1.SetPropertyBlock(_propBlock);
    }

    // 2. Sadece SR2 (Beyaz - İç Halka) ile ilgilenir
    private void UpdateInnerLight(CircleLight lightObj, float mainAlpha)
    {
        // SR2 yoksa veya kapalıysa işlem yapma
        if (lightObj == null || lightObj.SpriteRenderer2 == null || !lightObj.SpriteRenderer2.gameObject.activeSelf) return;

        _propBlock.Clear();
        lightObj.SpriteRenderer2.GetPropertyBlock(_propBlock);

        // RENK: Daima Beyaz
        Color whiteColor = Color.white;
        whiteColor.a = 1f;
        _propBlock.SetColor(ShapeColorId, whiteColor); // veya MainColorId, shaderına göre
        // _propBlock.SetColor(MainColorId, whiteColor); 

        // ALPHA: Gelen alpha * 1.0 (veya sr2MaxAlpha)
        float finalAlpha = mainAlpha * sr2MaxAlpha;
        _propBlock.SetFloat(AlphaId, finalAlpha);

        lightObj.SpriteRenderer2.SetPropertyBlock(_propBlock);
    }

    // --- Snake Efektlerinde de bu ayrımı kullanmak için yardımcı ---
    
    // Snake/DoubleSnake fonksiyonlarında "SetLightColorWithAlpha" çağırdığın yerleri
    // şununla güncellemen gerekecek (veya o metodu wrapper yapabilirsin):

    private void SetLightColorWithAlpha(CircleLight lightObj, float alpha)
    {
        UpdateOuterLight(lightObj, alpha);
        UpdateInnerLight(lightObj, alpha);
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