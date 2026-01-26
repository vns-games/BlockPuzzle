using UnityEngine;
using System.Collections;

public static class Vibration
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    public static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#else
    public static AndroidJavaClass unityPlayer;
    public static AndroidJavaObject currentActivity;
    public static AndroidJavaObject vibrator;
#endif

    /// <summary>
    /// Gelişmiş Titreşim
    /// </summary>
    /// <param name="milliseconds">Süre (ms cinsinden). Örn: 100</param>
    /// <param name="amplitude">Şiddet (0 ile 255 arası). Örn: 50 (Hafif), 255 (Sert)</param>
    public static void Vibrate(long milliseconds, int amplitude)
    {
        // Editörde çalışmaz
        if (Application.isEditor) return;

#if UNITY_ANDROID
        // Android API 26+ (Oreo ve üzeri) Şiddet kontrolünü destekler
        if (GetSdkVersion() >= 26)
        {
            try
            {
                // VibrationEffect.createOneShot(milliseconds, amplitude)
                // Amplitude: 1-255 arası. -1 ise varsayılan güç.
                AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");

                // amplitude değerini 255 ile sınırla
                amplitude = Mathf.Clamp(amplitude, 1, 255);

                AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                vibrator.Call("vibrate", vibrationEffect);
            }
            catch(System.Exception)
            {
                // Eski cihazlarda standart çalışsın
                Handheld.Vibrate();
            }
        }
        else
        {
            // Eski Android sürümlerinde sadece süre verilebilir
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS
        // iOS'ta Taptic Engine için ayrı plugin gerekir, şimdilik standart çalışır
        Handheld.Vibrate();
#else
        Handheld.Vibrate();
#endif
    }

    // Android SDK Versiyonunu kontrol eder
    private static int GetSdkVersion()
    {
        if (Application.isEditor) return 0;
#if UNITY_ANDROID
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return version.GetStatic<int>("SDK_INT");
        }
#else
        return 0;
#endif
    }

    // Titreşimi iptal et
    public static void Cancel()
    {
        if (Application.isEditor) return;
#if UNITY_ANDROID
        vibrator.Call("cancel");
#endif
    }
}
 // async/await için gerekli