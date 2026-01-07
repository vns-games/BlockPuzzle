using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 1. DOTween Kütüphanesini ekledik

[RequireComponent(typeof(PolygonCollider2D))]
public class DraggableBlock : MonoBehaviour
{
    // --- VERİ ---
    private BlockData _currentData;
    private bool _isDragging;
    private Vector3 _startPosition;

    // --- VISUAL & SETTINGS ---
    [Header("Visual")]
    public Transform visualRoot;
    public GameObject cellPrefab;
    
    [Header("Settings")]
    public float slotScale = 0.6f;
    public float dragScale = 1.0f;
    public float dragOffsetY = 1f;
    
    // Coroutine hızı yerine Süre (Duration) kullanıyoruz
    public float moveDuration = 0.3f; 

    // --- REFS ---
    private PolygonCollider2D _col;
    private GridManager _grid => GridManager.Instance;
    private BlockGhost _ghost => BlockGhost.Instance;

    void Awake()
    {
        _col = GetComponent<PolygonCollider2D>();
    }

    void Start()
    {
        _startPosition = transform.position;
        transform.localScale = Vector3.one * slotScale;
    }

    public void Initialize(BlockShapeSO so)
    {
        var matrix = so.ToMatrix().Trim();
        _currentData = new BlockData(matrix);

        RebuildVisual();
        RebuildCollider();
    }
    
    public BlockData GetData() => _currentData;

    // --- MERKEZLEME HESABI ---
    private Vector3 GetCenteringOffset()
    {
        if (_currentData == null) return Vector3.zero;
        float s = _grid.cellSize;
        
        float ox = (_currentData.Width * s) / 2f;
        float oy = (_currentData.Height * s) / 2f;
        
        return new Vector3(ox, oy, 0);
    }

    void RebuildVisual()
    {
        foreach (Transform c in visualRoot) Destroy(c.gameObject);
        if (_currentData == null) return;

        float s = _grid.cellSize;
        Vector3 offset = GetCenteringOffset();
        Vector3 halfCell = new Vector3(s / 2f, s / 2f, 0);

        for (int x = 0; x < _currentData.Width; x++)
        {
            for (int y = 0; y < _currentData.Height; y++)
            {
                if (!_currentData.Matrix[x, y]) continue;
                Vector3 pos = new Vector3(x * s, y * s, 0) + halfCell - offset;
                Instantiate(cellPrefab, visualRoot).transform.localPosition = pos;
            }
        }
    }

    void RebuildCollider()
    {
        if (_currentData == null) return;
        
        _col.pathCount = 0;
        List<Vector2[]> paths = new();
        float s = _grid.cellSize;
        Vector3 offset = GetCenteringOffset();

        for (int x = 0; x < _currentData.Width; x++)
        {
            for (int y = 0; y < _currentData.Height; y++)
            {
                if (!_currentData.Matrix[x, y]) continue;
                float px = (x * s) - offset.x;
                float py = (y * s) - offset.y;
                paths.Add(new Vector2[] {
                    new(px, py), new(px + s, py), new(px + s, py + s), new(px, py + s)
                });
            }
        }
        _col.pathCount = paths.Count;
        for (int i = 0; i < paths.Count; i++) _col.SetPath(i, paths[i]);
    }

    void Update()
    {
        if (_currentData == null) return;

        // --- 1. ROTASYON ---
        if (Input.GetKeyDown(KeyCode.R)) // Tutmasak da dönebilir
        {
            var rotated = _currentData.Matrix.RotateRight().Trim();
            _currentData.UpdateMatrix(rotated);
            RebuildVisual();
            RebuildCollider();
            
            // Dönünce ufak bir "Punch" efekti (Opsiyonel ama hoş durur)
            transform.DOKill(true); // Önceki animasyonu bitir
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1);
            // Boyut bozulursa diye tekrar set et
            float targetScale = _isDragging ? dragScale : slotScale;
            transform.localScale = Vector3.one * targetScale;
        }
        
        // Eğer tutmuyorsak aşağısı çalışmasın
        if (!_isDragging) return;

        // --- 2. HAREKET ---
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
        
        // Z=-5f ile her şeyin üstünde tutuyoruz
        transform.position = mouse + new Vector3(0, dragOffsetY, -5f);

        // --- 3. GRID KONTROLÜ (SNAP FIX) ---
        Vector3 originPos = transform.position - GetCenteringOffset();
        Vector3 snapFix = new Vector3(_grid.cellSize / 2f, _grid.cellSize / 2f, 0);

        Vector2Int cell = _grid.WorldToCell(originPos + snapFix);
        
        bool canPlace = _grid.CanPlace(_currentData, cell.x, cell.y);

        if (canPlace)
            _ghost.Show(_currentData, cell, _grid);
        else
            _ghost.Clear();

        // --- 4. BIRAKMA ---
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            
            if (canPlace)
            {
                _grid.PlacePiece(_currentData, cell.x, cell.y);
                _ghost.Clear();
                BlockSpawner.Instance.OnBlockPlaced(this);
                
                // DOTween ile güvenli yok etme
                transform.DOKill(); 
                Destroy(gameObject);
            }
            else
            {
                _ghost.Clear();
                
                // --- DOTWEEN İLE EVE DÖNÜŞ ---
                // Coroutine yerine tek satır. OutBack lastik gibi fırlatır.
                transform.DOMove(_startPosition, moveDuration).SetEase(Ease.OutBack);
                transform.DOScale(slotScale, moveDuration).SetEase(Ease.OutBack);
            }
        }
    }

    void OnMouseDown()
    {
        if (_currentData == null) return;
        _isDragging = true;
        
        // Önceki animasyonları durdur (Üst üste binmesin)
        transform.DOKill();
        
        // --- DOTWEEN İLE BÜYÜME ---
        transform.DOScale(dragScale, 0.2f).SetEase(Ease.OutBack);
    }
}