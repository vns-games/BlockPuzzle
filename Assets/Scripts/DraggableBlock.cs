using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

// ARTIK KUTU COLLIDER İSTİYORUZ
[RequireComponent(typeof(BoxCollider2D))]
public class DraggableBlock : MonoBehaviour
{
    // --- VERİ ---
    public BlockShapeSO SourceSO { get; private set; } 
    private BlockData _currentData;
    private bool _isDragging;
    private Vector3 _startPosition;
    private BlockColorType _myColor;
    
    [Header("Visual Settings")]
    public Transform visualRoot;
    public VisualCell visualCellPrefab;
    
    [Header("Hitbox & Scaling")]
    public Vector2 hitboxSize = new Vector2(4f, 4f); // Tıklama alanı boyutu (Büyük Kare)
    public float maxSlotDimension = 2.5f; // Slot içindeyken blok en fazla kaç birim yer kaplasın?
    public float dragOffsetY = 1.5f;      // Sürüklerken parmağın ne kadar üstünde olsun?
    public float moveDuration = 0.3f;  

    private List<VisualCell> _spawnedCells = new List<VisualCell>();

    private BoxCollider2D _col; // Polygon yerine Box
    private Vector3 _slotScale; // Hesaplanan küçültülmüş boyut
    
    private GridManager _grid => GridManager.Instance;
    private BlockGhost _ghost => BlockGhost.Instance;

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        // Collider'ı tetikleyici yap ki fizik motoruyla çarpışıp sağa sola uçmasın
        _col.isTrigger = true; 
    }

    void Start()
    {
        _startPosition = transform.position;
        // Başlangıçta hesaplanan slot ölçeğinde olsun (Initialize'da hesaplanacak)
    }

    public void Initialize(BlockShapeSO so)
    {
        SourceSO = so;
        var matrix = so.ToMatrix().Trim();
        _currentData = new BlockData(matrix);

        int colorCount = System.Enum.GetValues(typeof(BlockColorType)).Length;
        // Renk Initialize'da belirlenir ve sabit kalır
        _myColor = (BlockColorType)Random.Range(0, colorCount);

        RebuildVisual();
        UpdateHitboxAndScale(); // <-- YENİ FONKSİYON
    }
    
    public BlockData GetData() => _currentData;

    void RebuildVisual()
    {
        foreach (Transform c in visualRoot) Destroy(c.gameObject);
        _spawnedCells.Clear();

        if (_currentData == null) return;

        float s = _grid.cellSize;
        
        // Görselleri merkeze hizalamak için ofset hesabı
        // Bu sayede blok görseli (0,0) noktasında ortalanır
        Vector3 offset = new Vector3((_currentData.Width * s) / 2f, (_currentData.Height * s) / 2f, 0);
        Vector3 halfCell = new Vector3(s / 2f, s / 2f, 0);

        for (int x = 0; x < _currentData.Width; x++)
        {
            for (int y = 0; y < _currentData.Height; y++)
            {
                if (!_currentData.Matrix[x, y]) continue;

                Vector3 pos = new Vector3(x * s, y * s, 0) + halfCell - offset;
                VisualCell cell = Instantiate(visualCellPrefab, visualRoot);
                cell.transform.localPosition = pos;

                cell.Initialize(_myColor, 15, VisualSpawnType.Spawned);
                _spawnedCells.Add(cell);
            }
        }
    }

 // --- GÜNCELLENMİŞ AKILLI SIĞDIRMA ---
    void UpdateHitboxAndScale()
    {
        if (_currentData == null) return;

        // 1. COLLIDER (HITBOX) AYARI
        // Hitbox boyutunu ayarla.
        _col.offset = Vector2.zero;
        _col.size = hitboxSize;

        // 2. AKILLI ÖLÇEK HESABI
        float s = _grid.cellSize;
        
        // Bloğun ham (orijinal) boyutu
        float currentWidth = _currentData.Width * s;
        float currentHeight = _currentData.Height * s;
        float maxContentDim = Mathf.Max(currentWidth, currentHeight);

        // A) Manuel Limit: Inspector'da belirlediğin sınır (Örn: 2.5f)
        float limitSize = maxSlotDimension;

        // B) Ekran Limiti: Ekranın genişliğine göre dinamik sınır (YENİ)
        if (Camera.main != null)
        {
            // Ekranın dünya koordinatlarında genişliği
            float screenHeight = Camera.main.orthographicSize * 2;
            float screenWidth = screenHeight * Camera.main.aspect;
            
            // Ekranı 3 slot kabul edersek, her slota düşen güvenli pay (biraz da boşluk için 3.5'e böldük)
            float maxAllowedScreenSpace = screenWidth / 3.5f;

            // Eğer manuel limit (maxSlotDimension) ekrana sığmayacak kadar büyükse,
            // limiti ekran genişliğine göre daralt.
            if (limitSize > maxAllowedScreenSpace)
            {
                limitSize = maxAllowedScreenSpace;
            }
            
            // Dikey kontrol: Grid ile çakışmaması için dikey limiti de kısıtlayalım
            // Slotların genelde ekranın alt %20'sinde olduğunu varsayarsak:
            float maxAllowedHeight = screenHeight * 0.15f; 
            if (limitSize > maxAllowedHeight)
            {
                limitSize = maxAllowedHeight;
            }
        }

        // Ölçekleme Çarpanını Hesapla
        float scaleFactor = 1.0f;
        
        // Eğer blok bu limitten büyükse, limite sığacak kadar küçült
        if (maxContentDim > limitSize)
        {
            scaleFactor = limitSize / maxContentDim;
        }
        else
        {
            // Zaten küçükse (örn 1x1, 2x2), birazcık küçült ki şık dursun
            scaleFactor = 0.75f; 
        }

        _slotScale = Vector3.one * scaleFactor;
        
        // Hemen uygula
        transform.localScale = _slotScale;
    }

    void OnMouseDown()
    {
        if (_currentData == null) return;
        _isDragging = true;
        
        foreach (var cell in _spawnedCells) cell.OnDragging();

        // Tıklayınca GERÇEK BOYUTUNA (1:1) Büyüt
        transform.DOKill();
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        
        // Diğer blokların üstünde görünsün diye sorting order arttırılabilir
        // (VisualCell içinde sorting order yönetiliyorsa gerek yok)
    }

    void Update()
    {
        if (!_isDragging || _currentData == null) return;

        // Mouse Takibi (Ofsetli)
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0; 
        // dragOffsetY ile parmağın yukarısına kaldırıyoruz
        transform.position = new Vector3(mouse.x, mouse.y, -5f) + new Vector3(0, dragOffsetY, 0);

        // Ghost Hesaplama
        float s = _grid.cellSize;
        // Görseller ortalı olduğu için, bloğun sol alt köşesini (origin) bulurken yarım boyut kadar geri gidiyoruz
        Vector3 centerOffset = new Vector3((_currentData.Width * s) / 2f, (_currentData.Height * s) / 2f, 0);
        Vector3 origin = transform.position - centerOffset;
        
        // Grid snap düzeltmesi
        Vector3 snapFix = new Vector3(s / 2f, s / 2f, 0);
        Vector2Int cell = _grid.WorldToCell(origin + snapFix);
        
        bool canPlace = _grid.CanPlace(_currentData, cell.x, cell.y);

        if (canPlace)
        {
            // --- GHOST SPRITE GÖNDERME ---
            Sprite mySprite = null;
            var firstRenderer = GetComponentInChildren<SpriteRenderer>();
            if (firstRenderer != null) mySprite = firstRenderer.sprite;

            // Ghost'a Sprite'ı gönder
            _ghost.Show(_currentData, cell, _grid, _myColor, mySprite);
        }
        else _ghost.Clear();

        // Bırakma İşlemi
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
    
            if (canPlace)
            {
                // BAŞARILI
                foreach (var c in _spawnedCells) c.OnDrop();

                _ghost.Clear();
                BlockSpawner.Instance.OnBlockPlaced(this);

                _grid.PlacePiece(_currentData, cell.x, cell.y, _myColor); 
        
                transform.DOKill(); 
                Destroy(gameObject); 
            }
            else
            {
                // BAŞARISIZ: Yuvaya Dönüş
                foreach (var c in _spawnedCells)
                {
                    c.OnDrop(); 
                    c.OnIdle(); 
                }

                _ghost.Clear();
                
                // Eski yerine ve SLOT BOYUTUNA (_slotScale) dön
                transform.DOMove(_startPosition, moveDuration).SetEase(Ease.OutBack);
                transform.DOScale(_slotScale, moveDuration).SetEase(Ease.OutBack);
            }
        }
    }
}