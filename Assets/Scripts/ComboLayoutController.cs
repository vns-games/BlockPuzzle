using UnityEngine;
using DG.Tweening;
using VnS.Utility.Singleton;

public class ComboLayoutController : Singleton<ComboLayoutController>
{
    [Header("Sprite Referansları")]
    public SpriteRenderer comboTextSprite;
    public SpriteRenderer tensDigitSprite;
    public SpriteRenderer onesDigitSprite;

    [Header("Partikül")]
    public ParticleSystem hitParticle;
    public float particleZOffset = 1.0f;

    [Header("Hedef Boyutlar (Scale)")]
    public Vector3 comboTargetScale = new Vector3(0.3f, 0.3f, 1f);
    public Vector3 numberTargetScale = new Vector3(0.1f, 0.1f, 1f);

    [Header("Mesafe Ayarları")]
    public float mainSpacing = 0.2f;   // "Combo" yazısı ile Rakamlar arasındaki boşluk
    public float digitSpacing = 0.05f; // Rakamların (1 ve 1) kendi arasındaki boşluk (Daha dar olmalı)
    // YENİ: Yazı bloğun tam ortasında değil, biraz üstünde çıksın. Z'yi -5 yaparak öne alıyoruz.
    public Vector3 visualOffset = new Vector3(0, 1.5f, -5f);

    [Header("Ayarlar")]
    public Sprite[] numberSprites;

    [Header("Animasyon Zamanlaması")]
    public float textDuration = 0.3f;
    public float numberDuration = 0.2f;
    public float stayDuration = 1.5f;
    public float exitDuration = 0.2f;

    [Header("Ease Ayarları")]
    public Ease entryEase = Ease.OutBack;
    public Ease exitEase = Ease.InBack;

    private Sequence currentSequence;

    // YENİ PARAMETRE: Vector3 hitPos -> Parçayı koyduğun yerin dünya koordinatı
    public void SetComboValue(int value, Vector3 hitPos)
    {
        if (currentSequence != null) currentSequence.Kill();

        // 1. ÖNCE KONUMU AYARLA
        // Combo objesini (bu scriptin olduğu objeyi) vuruş noktasına taşıyoruz.
        transform.position = hitPos + visualOffset;

        // 2. Ölçekleri ayarla, sprite'ları seç, hizala
        ResetScalesToTarget();
        SetupSprites(value);
        AlignSprites(value);

        // 3. Partikül pozisyonunu bul (Artık yeni konuma göre hesaplayacak)
        Vector3 particleTargetPos = CalculateParticlePosition(value);

        // 4. Animasyonu Başlat
        PlayAnimation(particleTargetPos);
    }

    // --- BURADAN AŞAĞISI AYNI ---
    // (Kodun geri kalanı değişmedi, kopyala-yapıştır yapabilirsin)

    private void SetupSprites(int value)
    {
        // 1. Onlar Basamağı: 10 ve üzeri ise aç
        if (value >= 10)
        {
            tensDigitSprite.gameObject.SetActive(true);
            tensDigitSprite.sprite = numberSprites[value / 10];
        }
        else
        {
            tensDigitSprite.gameObject.SetActive(false);
        }

        // 2. Birler Basamağı: DEĞİŞİKLİK BURADA
        // Eğer değer 1'den büyükse göster, 1 ise GİZLE.
        if (value > 1) 
        {
            onesDigitSprite.gameObject.SetActive(true);
            onesDigitSprite.sprite = numberSprites[value % 10];
        }
        else 
        {
            // Sadece "Combo" yazısı kalsın istiyoruz
            onesDigitSprite.gameObject.SetActive(false);
        }
    }
    private void ResetScalesToTarget()
    {
        comboTextSprite.transform.localScale = comboTargetScale;
        tensDigitSprite.transform.localScale = numberTargetScale;
        onesDigitSprite.transform.localScale = numberTargetScale;
    }
    private void AlignSprites(int value)
    {
        // Combo yazısının genişliği her zaman var
        float comboW = comboTextSprite.bounds.size.x;
        
        // Rakamların genişliklerini varsayılan olarak 0 alalım
        float onesEffectiveW = 0f;
        float tensEffectiveW = 0f;

        // EĞER AÇIKLARSA genişliklerini hesapla
        if (onesDigitSprite.gameObject.activeSelf)
        {
            float onesRealW = onesDigitSprite.bounds.size.x;
            int onesDigit = value % 10;
            // "1" rakamı inceltme mantığı (skinnyDigitMultiplier)
            onesEffectiveW = (onesDigit == 1) ? onesRealW * .4f : onesRealW;
        }

        if (tensDigitSprite.gameObject.activeSelf)
        {
            float tensRealW = tensDigitSprite.bounds.size.x;
            int tensDigit = value / 10;
            tensEffectiveW = (tensDigit == 1) ? tensRealW * .4f : tensRealW;
        }

        // --- TOPLAM GENİŞLİK HESABI ---
        float totalWidth = comboW;

        // Birler basamağı varsa boşluk ve genişliğini ekle
        if (onesDigitSprite.gameObject.activeSelf)
        {
            totalWidth += mainSpacing + onesEffectiveW;
        }

        // Onlar basamağı varsa boşluk ve genişliğini ekle
        if (tensDigitSprite.gameObject.activeSelf) 
        {
            // Onlar basamağı ile birler basamağı arasındaki boşluk
            totalWidth += tensEffectiveW + digitSpacing;
        }

        // --- KONUMLANDIRMA ---
        float currentX = -(totalWidth / 2);

        // A. Combo Yazısı
        comboTextSprite.transform.localPosition = new Vector3(currentX + (comboW / 2), 0, 0);
        
        // Sadece birler basamağı açıksa imleci ilerlet
        if (onesDigitSprite.gameObject.activeSelf)
        {
            currentX += comboW + mainSpacing;
        }

        // B. Onlar Basamağı (Varsa)
        if (tensDigitSprite.gameObject.activeSelf)
        {
            tensDigitSprite.transform.localPosition = new Vector3(currentX + (tensEffectiveW / 2), 0, 0);
            currentX += tensEffectiveW + digitSpacing;
        }

        // C. Birler Basamağı (Varsa)
        if (onesDigitSprite.gameObject.activeSelf)
        {
            onesDigitSprite.transform.localPosition = new Vector3(currentX + (onesEffectiveW / 2), 0, 0);
        }
    }
    private Vector3 CalculateParticlePosition(int value)
    {
        Vector3 targetPos;
        if (value < 10) targetPos = onesDigitSprite.transform.position;
        else targetPos = (tensDigitSprite.transform.position + onesDigitSprite.transform.position) / 2f;
        targetPos.z += particleZOffset;
        return targetPos;
    }

   private void PlayAnimation(Vector3 particlePos)
    {
        currentSequence = DOTween.Sequence();

        // Hazırlık
        comboTextSprite.transform.localScale = Vector3.zero;
        tensDigitSprite.transform.localScale = Vector3.zero;
        onesDigitSprite.transform.localScale = Vector3.zero;

        // 1. Combo Yazısı Gelsin
        currentSequence.Append(comboTextSprite.transform.DOScale(comboTargetScale, textDuration).SetEase(entryEase));

        // 2. Rakamlar (SADECE AKTİFSE ANİMASYONA EKLE)
        if (tensDigitSprite.gameObject.activeSelf)
        {
            currentSequence.Append(tensDigitSprite.transform.DOScale(numberTargetScale, numberDuration).SetEase(entryEase));
            // Onlar varsa birler de kesin vardır
            if (onesDigitSprite.gameObject.activeSelf)
                currentSequence.Join(onesDigitSprite.transform.DOScale(numberTargetScale, numberDuration).SetEase(entryEase));
        }
        else if (onesDigitSprite.gameObject.activeSelf)
        {
            // Sadece birler basamağı varsa
            currentSequence.Append(onesDigitSprite.transform.DOScale(numberTargetScale, numberDuration).SetEase(entryEase));
        }

        // Partikül (Rakam yoksa direkt Combo yazısıyla patlasın)
        float particleTime = onesDigitSprite.gameObject.activeSelf ? textDuration : 0.1f;
        
        currentSequence.InsertCallback(particleTime, () => {
            if (hitParticle != null)
            {
                // Rakam yoksa Combo yazısının üzerine patlasın
                Vector3 finalParticlePos = onesDigitSprite.gameObject.activeSelf ? particlePos : comboTextSprite.transform.position;
                finalParticlePos.z = particlePos.z; // Z derinliğini koru
                
                hitParticle.transform.position = finalParticlePos;
                hitParticle.Stop();
                hitParticle.Play();
            }
        });

        // Bekleme ve Çıkış
        currentSequence.AppendInterval(stayDuration);

        // Çıkış Animasyonu
        currentSequence.Append(comboTextSprite.transform.DOScale(0f, exitDuration).SetEase(exitEase));
        
        if (tensDigitSprite.gameObject.activeSelf)
            currentSequence.Join(tensDigitSprite.transform.DOScale(0f, exitDuration).SetEase(exitEase));
            
        if (onesDigitSprite.gameObject.activeSelf)
            currentSequence.Join(onesDigitSprite.transform.DOScale(0f, exitDuration).SetEase(exitEase));
    }
}