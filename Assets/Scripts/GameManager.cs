using UnityEngine;
using VnS.Utility.Singleton;

public class GameManager : Singleton<GameManager>
{
    private bool _hasUsedRevive = false; // Bu oyun içinde dirilme hakkını kullandı mı?

    public void StartGame()
    {
        _hasUsedRevive = false;
        // Diğer başlatma kodları...
    }

    public void TriggerGameOver()
    {
        if (_hasUsedRevive)
        {
            // Zaten hakkını kullandıysa -> GERÇEK GAME OVER
            Debug.Log("Oyun bitti. Skor ekranı açılıyor.");
            // UIManager.Instance.ShowLoseScreen();
        }
        else
        {
            // Hakkı var -> REKLAM TEKLİFİ
            Debug.Log("Öldün! Ama reklam izlersen 1x1 bloklarla devam edebilirsin.");
            
            // Burası UI Manager'ı tetikler:
            //UIManager.Instance.ShowReviveOfferUI(); 
        }
    }

    // UI'daki "Reklamı İzle" butonu buna bağlanacak (Reklam başarıyla bitince)
    public void OnReviveSuccess()
    {
        _hasUsedRevive = true;
        Debug.Log("Reklam izlendi, oyun devam ediyor.");

        // YENİ: Hem güvenli blokları ver hem de WarmUp modunu 60sn aç.
        BlockSpawner.Instance.ActivateReviveMode();

        // UIManager.Instance.HideReviveUI();
    }

    // UI'daki "Hayır, İstemiyorum" butonu buna bağlanacak
    public void OnReviveDeclined()
    {
        Debug.Log("Teklif reddedildi. Gerçek Game Over.");
        // UIManager.Instance.ShowLoseScreen();
    }
}