using UnityEngine;
using UnityEngine.U2D; // SpriteAtlas için şart

public static class GameAssets
{
    private static SpriteAtlas _atlas;

    // Bu özelliğe (Property) her erişildiğinde:
    // Atlas henüz yüklenmediyse yükler, yüklüyse var olanı verir (Singleton mantığı).
    public static SpriteAtlas Atlas
    {
        get
        {
            if (_atlas == null)
            {
                // "BlockAtlas" ismi Resources klasöründeki dosya adıyla AYNI olmalı
                _atlas = Resources.Load<SpriteAtlas>("Atlas");
                
                if (_atlas == null) 
                    Debug.LogError("KRİTİK HATA: Resources klasöründe 'Atlas' bulunamadı!");
            }
            return _atlas;
        }
    }

    // İleride ses dosyalarını da böyle çekebilirsin:
    // public static AudioClip WinSound => ...
}