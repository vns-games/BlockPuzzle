using System.Threading.Tasks;
using UnityEngine;
public static class VibrationManager
{
    // Ayarlar (Static olduğu için her yerden direkt erişilebilir)
    public static bool IsEnabled;
    static VibrationManager()
    {
        IsEnabled = PlayerPrefs.GetInt("vibration_on", 1) == 1;
    }

    /// <summary>
    /// Titreşimi tetikler.
    /// </summary>
    public static void Trigger(int linesCleared, bool isFullClear)
    {
        // 1. Kapalıysa çalışma
        if (!IsEnabled) return;

        // 2. Platform kontrolü (Sadece mobil)
        #if !UNITY_ANDROID && !UNITY_IOS
            return;
        #endif

        // --- MANTIK ---

        // DURUM A: AĞIR YIKIM (3+ Satır veya Full Clear)
        if (isFullClear || linesCleared >= 3)
        {
            // Coroutine yerine Async metodu çağırıyoruz (Fire and forget)
            PlayHeavyVibration();
        }
        // DURUM B: ORTA YIKIM (2 Satır)
        else if (linesCleared >= 2)
        {
            // Orta şiddet (50ms, 80 şiddet)
            Vibration.Vibrate(50, 80);
        }

        // 1 satır için titreşim yok
    }

    // Coroutine yerine "Async Void" kullanıyoruz.
    // Bu sayede MonoBehaviour olmadan zamanlamalı işlem yapabiliriz.
    private static async void PlayHeavyVibration()
    {
        // 1. Vuruş (Sert)
        Vibration.Vibrate(80, 255);

        // Bekleme (100 milisaniye)
        // Task.Delay, oyunun Time.timeScale'inden etkilenmez, gerçek zamanı sayar.
        await Task.Delay(120);

        // Eğer bu sırada oyuncu titreşimi kapattıysa veya oyun kapandıysa dur
        if (!Application.isPlaying || !IsEnabled) return;

        // 2. Vuruş (Sert)
        Vibration.Vibrate(80, 255);
    }

    // UI Toggle için
    public static void SetEnabled(bool state)
    {
        IsEnabled = state;
    }
}