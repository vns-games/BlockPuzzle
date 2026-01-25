using UnityEngine;
using System.Collections.Generic;

public static class Sound
{
    private static AudioSource _audioSource;
    private static Dictionary<string, SoundItem> _soundDict;
    private static GameObject _soundGameObject;

    // --- KURULUM (Otomatik Çalışır) ---
    private static void Initialize()
    {
        if (_audioSource != null) return; // Zaten kuruluysa çık

        // 1. GameObject Oluştur
        _soundGameObject = new GameObject("--- AUDIO_SYSTEM_AUTO ---");
        Object.DontDestroyOnLoad(_soundGameObject); // Sahne değişse de silinmesin

        // 2. AudioSource Ekle
        _audioSource = _soundGameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        // 3. Ses Dosyasını Resources'tan Yükle
        // Dosya adı "GameSounds" olmalı ve Resources klasöründe durmalı!
        var collection = Resources.Load<SoundCollectionSO>("GameSounds");

        if (collection == null)
        {
            Debug.LogError("KRİTİK HATA: 'Resources/GameSounds' dosyası bulunamadı!");
            return;
        }

        // 4. Dictionary Oluştur (Hızlı Erişim İçin)
        _soundDict = new Dictionary<string, SoundItem>();
        foreach (var item in collection.sounds)
        {
            if (!_soundDict.ContainsKey(item.key))
            {
                _soundDict.Add(item.key, item);
            }
        }

        Debug.Log("SES SİSTEMİ: Otomatik kuruldu ve hazır.");
    }

    // --- ÇALMA FONKSİYONU ---
    public static void Play(string key)
    {
        // Sistem kurulu değilse önce kur
        if (_audioSource == null) Initialize();

        if (_soundDict != null && _soundDict.TryGetValue(key, out SoundItem item))
        {
            // Pitch ve Volume ayarlarını yap
            _audioSource.pitch = item.pitch;

            // Çal
            _audioSource.PlayOneShot(item.clip, item.volume);
        }
        else
        {
            // Ses bulunamazsa konsolu kirletmesin, geliştirici görsün yeter
            // Debug.LogWarning($"Ses bulunamadı: {key}");
        }
    }

    public static void ToggleMute()
    {
        var mutePref = PlayerPrefs.GetInt("mute", 0);
        int muteStatus = Mathf.Abs(mutePref - 1);
        PlayerPrefs.SetInt("mute", muteStatus);

        bool isMuted = muteStatus == 1;

        _audioSource.mute = isMuted;
    }
}