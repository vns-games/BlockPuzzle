using UnityEngine;
[System.Serializable]
public class SoundItem
{
    public string key;                         // Çağırmak için kullanacağın isim (Örn: "Combo1")
    public AudioClip clip;                     // Ses dosyası
    [Range(0f, 1f)] public float volume = 1f;  // Her sesin kendi ses seviyesi olabilir
    [Range(0.1f, 3f)] public float pitch = 1f; // İnce/Kalın ayarı
}