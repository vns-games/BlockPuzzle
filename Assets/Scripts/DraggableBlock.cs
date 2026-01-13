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
    
    // Rengi burada tutuyoruz ve her yerde bunu kullanacağız
    private BlockColorType _myColor;
    
    [Header("Visual Settings")]
    public Transform visualRoot;
    public VisualCell visualCellPrefab;
    
    [Header("Behavior Settings")]
    public float slotScale = 0.6f;     
    public float dragScale = 1.0f;     
    public float dragOffsetY = 1f;     
    public float moveDuration = 0.3f;  

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

        // 1. Rengi BURADA seçiyoruz ve _myColor değişkenine kaydediyoruz.
        int colorCount = System.Enum.GetValues(typeof(BlockColorType)).Length;
        _myColor = (BlockColorType)Random.Range(0, colorCount);

        RebuildVisual();
        RebuildCollider();
    }
    
    public BlockData GetData() => _currentData;

    void RebuildVisual()
    {
        foreach (Transform c in visualRoot) Destroy(c.gameObject);
        _spawnedCells.Clear();

        if (_currentData == null) return;

        // DÜZELTME: Burada tekrar rastgele renk üretmiyoruz!
        // Initialize içinde seçilen _myColor'ı kullanıyoruz.

        float s = _grid.cellSize;
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

                // DÜZELTME: Yerel 'randomColor' yerine sınıf değişkeni '_myColor' gönderildi.
                // Böylece görünen renk ile grid'e giden renk aynı oldu.
                cell.Initialize(_myColor, 10, VisualSpawnType.Spawned);

                _spawnedCells.Add(cell);
            }
        }
    }

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

        // Ghost
        Vector3 originPos = transform.position - visualRoot.localPosition;
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
                // BAŞARILI: Gride Yerleşti
                foreach (var c in _spawnedCells) c.OnDrop();

                _ghost.Clear();
                BlockSpawner.Instance.OnBlockPlaced(this);

                // Rengi de gönderiyoruz (_myColor artık RebuildVisual'daki renkle aynı)
                _grid.PlacePiece(_currentData, cell.x, cell.y, _myColor); 
        
                transform.DOKill(); 
                Destroy(gameObject); 
            }
            else
            {
                // BAŞARISIZ: Yuvaya Dönüş
                foreach (var c in _spawnedCells)
                {
                    c.OnDrop(); // Glow kapansın
                    c.OnIdle(); // Normale dönsün
                }

                _ghost.Clear();
                transform.DOMove(_startPosition, moveDuration).SetEase(Ease.OutBack);
                transform.DOScale(slotScale, moveDuration).SetEase(Ease.OutBack);
            }
        }
    }
}