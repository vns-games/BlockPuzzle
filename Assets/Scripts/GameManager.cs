using UnityEngine;
using UnityEngine.SceneManagement;
using VnS.Utility.Singleton;
public class GameManager : Singleton<GameManager>
{
    [Header("UI References")]
    public GameObject gameOverPanel; // Inspector'dan atanacak Panel

    // Oyun durumunu takip edebiliriz
    public bool IsGameOver { get; private set; }

    public void TriggerGameOver()
    {
        if (IsGameOver) return; // Zaten bittiyse tekrar tetikleme
        
        IsGameOver = true;
        Debug.Log("GAME OVER!");

        // 1. Paneli aç
        if (gameOverPanel != null) 
            gameOverPanel.SetActive(true);

        // 2. İstersen blokların sürüklenmesini engelleyebilirsin
        // (BlockInput gibi bir scriptin varsa disable edersin)
    }

    public void RestartGame()
    {
        // Şu anki sahneyi baştan yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}