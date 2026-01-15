using UnityEngine;
using TMPro;
using VnS.Utility.Animation; // Eğer UI kullanıyorsan
using VnS.Utility.Singleton;

public class ScoreManager : Singleton<ScoreManager>
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText; // Puanın yazdığı Text
    public TextMeshProUGUI comboText; // "Combo x2!" yazan Text

    [Header("Settings")]
    public int pointsPerBlock = 5;   // Her kare 5 puan
    public int maxMovesForCombo = 3; // Kombo için tolerans (hamle sayısı)

    private int _currentScore = 0;
    private int _multiplier = 1;
    private int _movesSinceLastClear = 0;

    /// <summary>
    /// Oyuncu her blok yerleştirdiğinde bunu çağır.
    /// </summary>
    public void RegisterMove()
    {
        _movesSinceLastClear++;

        // Eğer çok beklersen kombo riskte (ama patlatana kadar sıfırlamıyoruz, belki şimdi patlatır)
        // İstersen burada UI güncelleyip "Kombo gidiyor!" diyebilirsin.
    }

    /// <summary>
    /// Patlatma olduğunda çağır.
    /// </summary>
    /// <param name="clearedBlockCount">Toplam silinen kare sayısı</param>
    public void OnBlast(int clearedBlockCount)
    {
        // --- KOMBO MANTIĞI ---
        // Eğer son patlatmadan beri 3 veya daha az hamle yapılmışsa kombo artar.
        if (_movesSinceLastClear <= maxMovesForCombo)
        {
            _multiplier++;
        }
        else
        {
            // Çok oyalandı, kombo başa döndü
            _multiplier = 1;
        }

        if (_multiplier >= 2)
        {
            if (GridManager.Instance)
                GridManager.Instance.ShakeGrid(.1f);
        }
        // --- PUAN HESABI ---
        // (Kare Sayısı * 5) * Çarpan
        int points = (clearedBlockCount * pointsPerBlock) * _multiplier;

        _currentScore += points;

        Debug.Log($"[SKOR] {clearedBlockCount} kare silindi. Çarpan: x{_multiplier}. Puan: +{points}");

        if (scoreText)
        {
            scoreText.ChangeNumber(points, "SCORE: ");
        }

        // Patlatma yapıldığı için sayacı sıfırla
        _movesSinceLastClear = 0;

        UpdateUI();
    }

    private void UpdateUI()
    {

        if (comboText)
        {
            if (_multiplier > 1)
            {
                comboText.text = $"x{_multiplier}";
                comboText.gameObject.SetActive(true);
                // Buraya DOTween punch efekti eklersen çok tatlı olur
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }


}