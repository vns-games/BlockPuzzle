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
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int ShapeColorId = Shader.PropertyToID("_Shape2Color");
    private static readonly int MainColorId = Shader.PropertyToID("_Color"); // Standart Shaderlar için
    
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
                ChangeBaseColor(new Color(1, .8f, 0)); // Sarı
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

        breatheTween = DOVirtual.Float(minAlpha, 1f, 2f, (value) => 
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

    // --- KRİTİK DEĞİŞİKLİK BURADA ---
    private void SetLightColorWithAlpha(CircleLight lightObj, float alpha)
    {
        if (lightObj == null) return;

        // --- 1. SpriteRenderer1 (RENKLİ KISIM) ---
        if (lightObj.SpriteRenderer1 != null)
        {
            // PropertyBlock'u al
            lightObj.SpriteRenderer1.GetPropertyBlock(_propBlock);

            // MATEMATİK: Siyah'tan -> Hedef Renge geçiş yap.
            // alpha 0 ise Siyah (0,0,0), alpha 1 ise _currentBaseColor döner.
            Color finalColor = Color.Lerp(Color.black, _currentBaseColor, alpha);

            // ÖNEMLİ: Shader'ın "Alpha Cutoff" yapıp cismi tamamen yok etmemesi için
            // Alpha kanalını 1 (Opak) tutuyoruz. Biz ışığı SİYAH yaparak söndürüyoruz.
            finalColor.a = 1f; 

            // Değerleri Ata
            //_propBlock.SetColor(GlowColorId, finalColor);
            //_propBlock.SetColor(ShapeColorId, finalColor);
            _propBlock.SetColor(MainColorId, finalColor); // Standart shaderlar için

            // Uygula
            lightObj.SpriteRenderer1.SetPropertyBlock(_propBlock);
        }

        // --- 2. SpriteRenderer2 (BEYAZ KISIM - Eğer Kullanırsan) ---
        // Şu an kapalı olsa bile mantığı aynı kuralım: Beyaz -> Siyah
        if (lightObj.SpriteRenderer2 != null && lightObj.SpriteRenderer2.gameObject.activeSelf)
        {
            lightObj.SpriteRenderer2.GetPropertyBlock(_propBlock);

            // Beyazdan siyaha geçiş
            Color finalWhite = Color.Lerp(Color.black, Color.white, alpha);
            finalWhite.a = 1f; // Alpha hep full, renk kararıyor

            //_propBlock.SetColor(GlowColorId, finalWhite);
            _propBlock.SetColor(ShapeColorId, finalWhite);
            //_propBlock.SetColor(MainColorId, finalWhite);

            lightObj.SpriteRenderer2.SetPropertyBlock(_propBlock);
        }
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