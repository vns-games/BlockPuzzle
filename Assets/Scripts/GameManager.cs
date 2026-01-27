using UnityEngine;
using UnityEngine.Events;
using VnS.Utility.Singleton;

public class GameManager : Singleton<GameManager>
{
    private bool _hasUsedRevive = false; // Bu oyun içinde dirilme hakkını kullandı mı?
    [SerializeField] private UnityEvent onNotUsedRevive, onGameOver;

    protected override void Awake()
    {
        Application.targetFrameRate = 60;
        base.Awake();
    }
    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        _hasUsedRevive = false;
        Music.Play();
        Debug.Log("GAME MANAGER: Başlatma sekansı çalışıyor...");
        ScoreManager.Instance.Initialize();
        // 2. SONRA GRİDİ OLUŞTUR (Havuzdan parça çekecek)
        GridManager.Instance.Initialize();

        Debug.Log("GAME MANAGER: Oyun hazır!");
    }

    public void TriggerGameOver()
    {
        if (_hasUsedRevive)
        {
            // Zaten hakkını kullandıysa -> GERÇEK GAME OVER
            onGameOver?.Invoke();
        }
        else
        {
            onNotUsedRevive?.Invoke();
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
}