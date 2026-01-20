using System;
using System.Collections;
using UnityEngine;
using TMPro;
using VnS.Utility.Animation; // Eğer UI kullanıyorsan
using VnS.Utility.Singleton;

public class ScoreManager : Singleton<ScoreManager>
{
    public static event Action<int> OnCombo, OnBestScoreChanged, OnScoreChanged;
    public static event Action OnIncredible, OnBestScore;
    [Header("UI References")]
    public TextMeshProUGUI scoreText;     // Puanın yazdığı Text
    public TextMeshProUGUI bestScoreText; // "Combo x2!" yazan Text

    [Header("Settings")]
    public int pointsPerBlock = 5;   // Her kare 5 puan
    public int maxMovesForCombo = 3; // Kombo için tolerans (hamle sayısı)

    private int _currentScore = 0;
    private int _multiplier = 1;
    private int _movesSinceLastClear = 100;
    public int Multiplier => _multiplier;
    private int _bestScore;

    private int BestScore
    {
        get => _bestScore;
        set
        {
            _bestScore = value;
            OnBestScoreChanged?.Invoke(_bestScore);
        }
    }
    public int CurrentScore
    {
        get => _currentScore;
        private set
        {
            _currentScore = value;
            OnScoreChanged?.Invoke(_currentScore);
        }
    }
    private bool _beatedBestScore;
    public void Initialize()
    {
        _beatedBestScore = false;
        _movesSinceLastClear = 100;
        _multiplier = 1;
        CurrentScore = 0;
        BestScore = PlayerPrefs.GetInt("BestScore");
    }

    /// <summary>
    /// Oyuncu her blok yerleştirdiğinde bunu çağır.
    /// </summary>
    public void RegisterMove()
    {
        _movesSinceLastClear++;

        // Eğer çok beklersen kombo riskte (ama patlatana kadar sıfırlamıyoruz, belki şimdi patlatır)
        // İstersen burada UI güncelleyip "Kombo gidiyor!" diyebilirsin.

        NotifyComboTimer();
    }

    /// <summary>
    /// Patlatma olduğunda çağır.
    /// </summary>
    /// <param name="clearedBlockCount">Toplam silinen kare sayısı</param>
    private int _comboAccumulator = 0;

    public void OnBlast(int clearedBlockCount, int clearedLinesCount, bool isFullClear)
    {
        // 1. KOMBO MANTIĞI
        if (_movesSinceLastClear <= maxMovesForCombo)
            _comboAccumulator += clearedLinesCount;
        else
            _comboAccumulator = 0;

        _multiplier = (_comboAccumulator > 0) ? _comboAccumulator : 1;

        // 2. COMBO SESİ (Gecikmesiz)
        if (_multiplier >= 1) PlayComboSound(_multiplier);


        // --- 3. VOKAL SESİ SEÇİMİ (BURASI DEĞİŞTİ) ---
        string vocalKey = "";

        // ÖNCELİK 1: Grid tamamen temizlendiyse -> INCREDIBLE
        if (isFullClear)
        {
            vocalKey = "Incredible";
            // İstersen Full Clear için ekstra puan da verebilirsin:
            // _currentScore += 1000; 
            Debug.Log("[EFFECT] CLEAN SWEEP! Grid tertemiz oldu.");
        }
        // ÖNCELİK 2: Satır sayısına göre diğerleri
        else if (clearedLinesCount >= 2)
        {
            vocalKey = clearedLinesCount switch
            {
                2 => "Good",
                3 => "Great",
                4 => "Perfect",
                5 => "Excellent",
                _ => "###"
            };
        }

        // Vokal varsa gecikmeli çal
        if (vocalKey != "")
        {
            StartCoroutine(PlayVocalDelayed(vocalKey, 0.4f));
        }


        // 4. SHAKE (Full Clear ise kesin salla, yoksa 3 satır kuralına bak)
        if (isFullClear || clearedLinesCount >= 3)
        {
            if (GridManager.Instance)
            {

                GridManager.Instance.ShakeGrid(0.12f);
            }
        }

        // 5. PUANLAMA (Full Clear bonusu eklenebilir)
        int points = (clearedBlockCount * pointsPerBlock) * _multiplier;
        if (isFullClear) points += 500; // Örnek: Temizleme bonusu 500 puan

        _currentScore += points;

        // UI
        if (scoreText) scoreText.ChangeNumber(points, "SCORE: ");

        if (_bestScore < _currentScore)
        {
            PlayerPrefs.SetInt("BestScore", _currentScore);

            if (bestScoreText)
            {
                bestScoreText.ChangeNumber(points, "BEST: ");
            }
            if (!_beatedBestScore)
            {
                OnBestScore?.Invoke();
                _beatedBestScore = true;
            }
        }


        _movesSinceLastClear = 0;

        NotifyComboTimer();
        if (isFullClear)
        {
            OnIncredible?.Invoke();
        }
    }
    // ScoreManager.cs içindeki ilgili fonksiyon

    private void PlayComboSound(int multiplier)
    {
        // 10'dan büyükse 10'a sabitle
        int soundIndex = Mathf.Clamp(multiplier, 1, 11);

        // Key oluştur: "Combo1", "Combo2"...
        string key = $"Combo{soundIndex}";
        print("Playing " + key);

        // TEK SATIRDA ÇAL:
        Sound.Play(key);
    }
    private IEnumerator PlayVocalDelayed(string key, float delay)
    {
        // Belirtilen süre kadar bekle (0.4 saniye idealdir)
        yield return new WaitForSeconds(delay);

        // Vokali çal
        Sound.Play(key);
    }

    private void NotifyComboTimer()
    {
        // Formül: Maksimum Hak - Harcanan Hak
        int remainingMoves = maxMovesForCombo - _movesSinceLastClear;

        // Eğer sonuç negatifse (kombo kırılmışsa veya oyun başıysa), 0'a sabitle
        // Böylece UI'da eksi değer görmeyiz.
        if (remainingMoves < 0) remainingMoves = 0;

        // Eventi fırlat! (Dinleyen herkes kalan süreyi öğrensin)
        OnCombo?.Invoke(remainingMoves);
    }

}