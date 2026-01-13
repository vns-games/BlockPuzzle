using UnityEngine;
using UnityEngine.U2D; // SpriteAtlas için şart

public static class GameAssets
{
    private static SpriteAtlas _blockAtlas;

    // Bu özelliğe (Property) her erişildiğinde:
    // Atlas henüz yüklenmediyse yükler, yüklüyse var olanı verir (Singleton mantığı).
    public static SpriteAtlas BlockAtlas
    {
        get
        {
            if (_blockAtlas == null)
            {
                // "BlockAtlas" ismi Resources klasöründeki dosya adıyla AYNI olmalı
                _blockAtlas = Resources.Load<SpriteAtlas>("BlockAtlas");
                
                if (_blockAtlas == null) 
                    Debug.LogError("KRİTİK HATA: Resources klasöründe 'BlockAtlas' bulunamadı!");
            }
            return _blockAtlas;
        }
    }

    // İleride ses dosyalarını da böyle çekebilirsin:
    // public static AudioClip WinSound => ...
}