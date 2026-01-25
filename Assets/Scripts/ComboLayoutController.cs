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
        AlignSprites();

        // 3. Partikül pozisyonunu bul (Artık yeni konuma göre hesaplayacak)
        Vector3 particleTargetPos = CalculateParticlePosition(value);

        // 4. Animasyonu Başlat
        PlayAnimation(particleTargetPos);
    }

    // --- BURADAN AŞAĞISI AYNI ---
    // (Kodun geri kalanı değişmedi, kopyala-yapıştır yapabilirsin)

    private void SetupSprites(int value)
    {
        if (value >= 10)
        {
            tensDigitSprite.gameObject.SetActive(true);
            tensDigitSprite.sprite = numberSprites[value / 10];
        }
        else tensDigitSprite.gameObject.SetActive(false);
        onesDigitSprite.gameObject.SetActive(true);
        onesDigitSprite.sprite = numberSprites[value % 10];
    }
    private void ResetScalesToTarget()
    {
        comboTextSprite.transform.localScale = comboTargetScale;
        tensDigitSprite.transform.localScale = numberTargetScale;
        onesDigitSprite.transform.localScale = numberTargetScale;
    }
    private void AlignSprites()
    {
        // 1. Genişlikleri al
        float comboW = comboTextSprite.bounds.size.x;
        float onesW = onesDigitSprite.bounds.size.x;
        float tensW = tensDigitSprite.gameObject.activeSelf ? tensDigitSprite.bounds.size.x : 0f;

        // 2. Toplam Genişliği Hesapla
        // Combo genişliği + Ana Boşluk + (Varsa Onlar Basamağı + Rakam Boşluğu) + Birler Basamağı
        float totalWidth = comboW + mainSpacing + onesW;
        
        if (tensDigitSprite.gameObject.activeSelf) 
        {
            totalWidth += tensW + digitSpacing; // Onlar basamağı varsa araya digitSpacing girer
        }

        // 3. Başlangıç X noktası (Ortalamak için en sola git)
        float currentX = -(totalWidth / 2);

        // --- A. COMBO YAZISI ---
        comboTextSprite.transform.localPosition = new Vector3(currentX + (comboW / 2), 0, 0);
        
        // İmleci sağa kaydır (Yazı genişliği + Ana Boşluk)
        currentX += comboW + mainSpacing;

        // --- B. ONLAR BASAMAĞI (Varsa) ---
        if (tensDigitSprite.gameObject.activeSelf)
        {
            tensDigitSprite.transform.localPosition = new Vector3(currentX + (tensW / 2), 0, 0);
            
            // İmleci sağa kaydır (Rakam genişliği + RAKAM BOŞLUĞU)
            currentX += tensW + digitSpacing;
        }

        // --- C. BİRLER BASAMAĞI ---
        onesDigitSprite.transform.localPosition = new Vector3(currentX + (onesW / 2), 0, 0);
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

        // --- 1. HAZIRLIK: HEPSİNİ SIFIRLA ---
        comboTextSprite.transform.localScale = Vector3.zero;
        tensDigitSprite.transform.localScale = Vector3.zero;
        onesDigitSprite.transform.localScale = Vector3.zero;

        // --- 2. GİRİŞ ANİMASYONU ---
        
        // A) "Combo" Yazısı Büyüsün (Append = Sıraya Koy)
        currentSequence.Append(comboTextSprite.transform.DOScale(comboTargetScale, textDuration).SetEase(entryEase));

        // B) Rakamlar Büyüsün
        if (tensDigitSprite.gameObject.activeSelf)
        {
            // Onlar basamağı (Append = Yazıdan SONRA başla)
            currentSequence.Append(tensDigitSprite.transform.DOScale(numberTargetScale, numberDuration).SetEase(entryEase));

            // Birler basamağı (Join = Onlar basamağıyla AYNI ANDA başla)
            // İŞTE ÇÖZÜM BURASI: Artık OnStart yerine Join kullanıyoruz.
            currentSequence.Join(onesDigitSprite.transform.DOScale(numberTargetScale, numberDuration).SetEase(entryEase));
        }
        else
        {
            // Sadece birler basamağı varsa (Append = Yazıdan SONRA başla)
            currentSequence.Append(onesDigitSprite.transform.DOScale(numberTargetScale, numberDuration).SetEase(entryEase));
        }

        // --- 3. PARTİKÜL TETİKLEME ---
        // Animasyonun "textDuration" süresi dolduğunda (yani rakamlar başlarken) partikülü patlat.
        currentSequence.InsertCallback(textDuration, () => {
            if (hitParticle != null)
            {
                hitParticle.transform.position = particlePos;
                hitParticle.Stop();
                hitParticle.Play();
            }
        });

        // --- 4. BEKLEME SÜRESİ ---
        currentSequence.AppendInterval(stayDuration);

        // --- 5. ÇIKIŞ ANİMASYONU (Yok Olma) ---
        // Combo yazısını küçült
        currentSequence.Append(comboTextSprite.transform.DOScale(0f, exitDuration).SetEase(exitEase));
        
        // Rakamları da onunla BERABER küçült (Join)
        if (tensDigitSprite.gameObject.activeSelf)
        {
            currentSequence.Join(tensDigitSprite.transform.DOScale(0f, exitDuration).SetEase(exitEase));
        }
        currentSequence.Join(onesDigitSprite.transform.DOScale(0f, exitDuration).SetEase(exitEase));
    }
}