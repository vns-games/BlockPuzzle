using UnityEngine;
using UnityEngine.Events;


public class VisualCell : MonoBehaviour
{
    [Header("Renderers")]
    public SpriteRenderer mainRenderer;
    public SpriteRenderer glowRenderer;

    [Header("Basic Events")]
    public UnityEvent OnIdleEvent;
    public UnityEvent OnDraggingEvent;
    public UnityEvent OnDropEvent;
    
    [Space(10)]
    [Header("Spawn Lifecycle Events")]
    public UnityEvent OnSpawnedEvent; // Spawner ürettiğinde (Pop-up animasyonu)
    public UnityEvent OnPlacedEvent;  // Gride yerleştiğinde (Toz/Sarsıntı)

    private BlockColorType _myColor;

    // Initialize artık "Nasıl Doğdum?" (VisualSpawnType) bilgisini de alıyor
    public void Initialize(BlockColorType colorType, int sortingOrder, VisualSpawnType spawnType)
    {
        _myColor = colorType;

        // --- Atlas ve Sprite Yükleme (GameAssets üzerinden) ---
        var atlas = GameAssets.BlockAtlas;
        if (atlas)
        {
            mainRenderer.sprite = atlas.GetSprite($"{colorType}");
            mainRenderer.sortingOrder = sortingOrder;
            
            glowRenderer.sprite = atlas.GetSprite($"GLOW_{colorType}");
            glowRenderer.sortingOrder = sortingOrder - 1;
            glowRenderer.gameObject.SetActive(false);
        }

        // --- HANGİ EVENT TETİKLENSİN? ---
        switch (spawnType)
        {
            case VisualSpawnType.None:
                // Sessiz açılış (Level Pattern)
                print("NONE SPAWNED: " + colorType);
                OnIdle(); 
                break;

            case VisualSpawnType.Spawned:
                // Slotlara yeni geldi
                OnIdle(); // Önce varsayılan duruma geç
                OnSpawnedEvent?.Invoke(); 
                break;

            case VisualSpawnType.Placed:
                // Gride yerleşti
                OnPlacedEvent?.Invoke();
                break;
        }
    }
    
    // Diğer state metodları...
    public void OnIdle() { if(glowRenderer) glowRenderer.gameObject.SetActive(false); OnIdleEvent?.Invoke(); }
    public void OnDragging() { if(glowRenderer) glowRenderer.gameObject.SetActive(true); OnDraggingEvent?.Invoke(); }
    public void OnDrop() { if(glowRenderer) glowRenderer.gameObject.SetActive(false); OnDropEvent?.Invoke(); }
}

public enum VisualSpawnType
{
    None,    // Level Pattern (Sessiz)
    Spawned, // Spawner üretti (Slotlara geliş efekti)
    Placed   // Oyuncu yerleştirdi (Gride oturma efekti)
}