using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class DraggableBlock : MonoBehaviour
{
    // --- VERİ ---
    public BlockShapeSO SourceSO { get; private set; } 
    private BlockData _currentData;
    private bool _isDragging;
    private Vector3 _startPosition;

    [Header("Visual Settings")]
    public Transform visualRoot;
    public VisualCell visualCellPrefab; // <--- TİP DEĞİŞTİ: GameObject yerine VisualCell
    
    [Tooltip("Rastgele seçilecek renkler")]
    public List<Sprite> blockSprites;

    [Header("Behavior Settings")]
    public float slotScale = 0.6f;     
    public float dragScale = 1.0f;     
    public float dragOffsetY = 1f;     
    public float moveDuration = 0.3f;  

    // Oluşturulan hücreleri burada tutuyoruz
    private List<VisualCell> _spawnedCells = new List<VisualCell>();

    private PolygonCollider2D _col;
    private GridManager _grid => GridManager.Instance;
    private BlockGhost _ghost => BlockGhost.Instance;

    void Awake()
    {
        _col = GetComponent<PolygonCollider2D>();
        _col.pathCount = 0; 
    }

    void Start()
    {
        _startPosition = transform.position;
        transform.localScale = Vector3.one * slotScale;
    }

    public void Initialize(BlockShapeSO so)
    {
        SourceSO = so;
        var matrix = so.ToMatrix().Trim();
        _currentData = new BlockData(matrix);

        RebuildVisual();
        RebuildCollider();
    }
    
    public BlockData GetData() => _currentData;

    void RebuildVisual()
    {
        // 1. Eskileri temizle
        foreach (Transform c in visualRoot) Destroy(c.gameObject);
        _spawnedCells.Clear();

        if (_currentData == null) return;

        // 2. Rastgele Sprite Seç
        Sprite selectedSprite = null;
        if (blockSprites != null && blockSprites.Count > 0)
            selectedSprite = blockSprites[Random.Range(0, blockSprites.Count)];

        float s = _grid.cellSize;
        // Ortalamak için offset hesabı
        Vector3 offset = new Vector3((_currentData.Width * s) / 2f, (_currentData.Height * s) / 2f, 0);
        Vector3 halfCell = new Vector3(s / 2f, s / 2f, 0);

        // 3. Hücreleri VisualCell scriptiyle oluştur
        for (int x = 0; x < _currentData.Width; x++)
        {
            for (int y = 0; y < _currentData.Height; y++)
            {
                if (!_currentData.Matrix[x, y]) continue;

                Vector3 pos = new Vector3(x * s, y * s, 0) + halfCell - offset;
                
                // Prefabı oluştur
                VisualCell cell = Instantiate(visualCellPrefab, visualRoot);
                cell.transform.localPosition = pos;

                // Hücreyi Başlat (Initialize)
                // Sorting Order'ı 10 yaptık, sen katmanına göre değiştirebilirsin.
                cell.Initialize(selectedSprite, 10);

                // Listeye ekle (Yönetmek için)
                _spawnedCells.Add(cell);
            }
        }
    }

    // --- YENİ COLLIDER OLUŞTURMA (Öncekiyle Aynı) ---
    void RebuildCollider()
    {
        if (_currentData == null) return;
        _col.pathCount = 1;
        float s = _grid.cellSize;
        Vector3 offset = new Vector3((_currentData.Width * s) / 2f, (_currentData.Height * s) / 2f, 0);
        float totalWidth = _currentData.Width * s;
        float totalHeight = _currentData.Height * s;

        Vector2[] points = new Vector2[]
        {
            new Vector2(-offset.x, -offset.y),
            new Vector2(totalWidth - offset.x, -offset.y),
            new Vector2(totalWidth - offset.x, totalHeight - offset.y),
            new Vector2(-offset.x, totalHeight - offset.y)
        };
        _col.SetPath(0, points);
    }

    void OnMouseDown()
    {
        if (_currentData == null) return;
        _isDragging = true;
        
        // 1. Tüm hücrelere "Sürükleniyorsun" komutu ver
        foreach (var cell in _spawnedCells) cell.OnDragging();

        transform.DOKill();
        transform.DOScale(dragScale, 0.2f).SetEase(Ease.OutBack);
    }

    void Update()
    {
        if (!_isDragging || _currentData == null) return;

        // Mouse Takibi
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0; 
        transform.position = new Vector3(mouse.x, mouse.y, -5f) + new Vector3(0, dragOffsetY, 0);

        // Ghost Gösterimi
        Vector3 originPos = transform.position - visualRoot.localPosition; // Offset düzeltmesi gerekebilir, visualRoot local 0 ise sorun yok.
        // Basitçe önceki logic:
        float s = _grid.cellSize;
        Vector3 centerOffset = new Vector3((_currentData.Width * s) / 2f, (_currentData.Height * s) / 2f, 0);
        Vector3 origin = transform.position - centerOffset;

        Vector3 snapFix = new Vector3(s / 2f, s / 2f, 0);
        Vector2Int cell = _grid.WorldToCell(origin + snapFix);
        
        bool canPlace = _grid.CanPlace(_currentData, cell.x, cell.y);

        if (canPlace) _ghost.Show(_currentData, cell, _grid);
        else _ghost.Clear();

        // Bırakma
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            
            if (canPlace)
            {
                // 2. Tüm hücrelere "Bırakıldın" komutu ver
                foreach (var c in _spawnedCells) c.OnDrop();

                _grid.PlacePiece(_currentData, cell.x, cell.y);
                _ghost.Clear();
                BlockSpawner.Instance.OnBlockPlaced(this);
                transform.DOKill(); 
                Destroy(gameObject); // Veya animasyonla yok et
            }
            else
            {
                // 3. Yerleşemedi, "Idle" moduna dön
                foreach (var c in _spawnedCells) c.OnIdle();

                _ghost.Clear();
                transform.DOMove(_startPosition, moveDuration).SetEase(Ease.OutBack);
                transform.DOScale(slotScale, moveDuration).SetEase(Ease.OutBack);
            }
        }
    }
}