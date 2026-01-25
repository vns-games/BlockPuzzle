using UnityEngine;
public static class Music
{
    private static AudioSource _audioSource;
    private static GameObject _musicGameObject;

    // --- KURULUM (Otomatik Çalışır) ---
    private static void Initialize()
    {
        if (_audioSource != null) return; // Zaten kuruluysa çık

        // 1. GameObject Oluştur
        _musicGameObject = new GameObject("--- MUSIC_SYSTEM_AUTO ---");
        Object.DontDestroyOnLoad(_musicGameObject); // Sahne değişse de silinmesin

        // 2. AudioSource Ekle
        _audioSource = _musicGameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = true;
        // 3. Ses Dosyasını Resources'tan Yükle
        // Dosya adı "GameSounds" olmalı ve Resources klasöründe durmalı!
        var clip = Resources.Load<AudioClip>("Music");

        if (clip == null)
        {
            Debug.LogError("KRİTİK HATA: 'Resources/Music' dosyası bulunamadı!");
            return;
        }
        _audioSource.clip = clip;

        // 4. Dictionary Oluştur (Hızlı Erişim İçin)
    }

    // --- ÇALMA FONKSİYONU ---
    public static void Play()
    {
        // Sistem kurulu değilse önce kur
        if (_audioSource == null) Initialize();
        _audioSource.loop = true;
        _audioSource.Play();
    }

    public static void ToggleMute()
    {
        var mutePref = PlayerPrefs.GetInt("music_mute", 0);
        int muteStatus = Mathf.Abs(mutePref - 1);
        PlayerPrefs.SetInt("music_mute", muteStatus);

        bool isMuted = muteStatus == 1;

        _audioSource.mute = isMuted;
    }
}