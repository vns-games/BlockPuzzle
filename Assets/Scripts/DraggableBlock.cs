using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class DraggableBlock : MonoBehaviour
{
    // --- VERİ ---
    public BlockShapeSO SourceSO { get; private set; } 
    private BlockData _currentData;
    private bool _isDragging;
    private Vector3 _startPosition;
    private BlockColorType _myColor;
    
    // --- HINT (İPUCU) ---
    private Vector2Int _hintPosition; 
    private BlockShapeSO _shapeData; 
    
    [Header("Visual Settings")]
    public Transform visualRoot;
    public VisualCell visualCellPrefab;
    
    [Header("Hitbox & Scaling")]
    public Vector2 hitboxSize = new Vector2(4f, 4f); 
    public float maxSlotDimension = 2.5f; 
    public float dragOffsetY = 2.0f; 
    public float moveDuration = 0.3f;  

    private List<VisualCell> _spawnedCells = new List<VisualCell>();
    private BoxCollider2D _col; 
    private Vector3 _slotScale; 
    
    // GridManager ve SENİN BLOCKGHOST SINIFIN
    private GridManager _grid => GridManager.Instance;
    private BlockGhost _ghost => BlockGhost.Instance; 

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true; 
    }

    void Start()
    {
        _startPosition = transform.position;
    }

    public void Initialize(BlockShapeSO so, Vector2Int bestFitPos)
    {
        _shapeData = so;
        _hintPosition = bestFitPos;
        SourceSO = so;
        
        var matrix = so.ToMatrix().Trim();
        _currentData = new BlockData(matrix);

        int colorCount = System.Enum.GetValues(typeof(BlockColorType)).Length;
        _myColor = (BlockColorType)Random.Range(0, colorCount);

        RebuildVisual();
        UpdateHitboxAndScale(); 
    }
    
    public BlockData GetData() => _currentData;

    void RebuildVisual()
    {
        foreach (Transform c in visualRoot) Destroy(c.gameObject);
        _spawnedCells.Clear();

        if (_currentData == null) return;

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

                cell.Initialize(_myColor, 15, VisualSpawnType.Spawned);
                _spawnedCells.Add(cell);
            }
        }
    }

    void UpdateHitboxAndScale()
    {
        if (_currentData == null) return;

        _col.offset = Vector2.zero;
        _col.size = hitboxSize;

        float s = _grid.cellSize;
        float currentWidth = _currentData.Width * s;
        float currentHeight = _currentData.Height * s;
        float maxContentDim = Mathf.Max(currentWidth, currentHeight);

        float limitSize = maxSlotDimension;

        if (Camera.main != null)
        {
            float screenHeight = Camera.main.orthographicSize * 2;
            float screenWidth = screenHeight * Camera.main.aspect;
            float maxAllowedScreenSpace = screenWidth / 3.5f;

            if (limitSize > maxAllowedScreenSpace) limitSize = maxAllowedScreenSpace;
            
            float maxAllowedHeight = screenHeight * 0.15f; 
            if (limitSize > maxAllowedHeight) limitSize = maxAllowedHeight;
        }

        float scaleFactor = 1.0f;
        if (maxContentDim > limitSize) scaleFactor = limitSize / maxContentDim;
        else scaleFactor = 0.75f; 

        _slotScale = Vector3.one * scaleFactor;
        transform.localScale = _slotScale;
    }

    void OnMouseDown()
    {
        if (_currentData == null) return;
        _isDragging = true;
        Sound.Play("Pick");
        foreach (var cell in _spawnedCells) cell.OnDragging();

        // Tıklayınca Büyüt
        transform.DOKill();
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        
        // --- İPUCU HAYALETİ (OPSİYONEL) ---
        // Eğer tıklayınca hemen Spawner'ın ipucunu (hedefi) da görmek istiyorsan bunu açabilirsin.
        // Ama senin BlockGhost zaten sürüklerken çalışacak, o yüzden kapalı kalabilir.
        /* if(BlockSpawner.Instance)
            BlockSpawner.Instance.ShowGhost(_shapeData, _hintPosition);
        */
    }

    void Update()
    {
        if (!_isDragging || _currentData == null) return;

        // 1. Mouse Takibi
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0; 
        transform.position = new Vector3(mouse.x, mouse.y, -5f) + new Vector3(0, dragOffsetY, 0);

        // 2. Anlık Konum Hesabı
        Vector2Int currentGridPos = CalculateGridPosition();

        // 3. Oraya Sığar mı?
        bool canPlace = _grid.CanPlace(_currentData, currentGridPos.x, currentGridPos.y);

        // --- BLOCK GHOST ENTEGRASYONU ---
        if (canPlace)
        {
            // Bloğun üzerindeki resmi (Sprite) al
            Sprite mySprite = null;
            // VisualCell prefabının içinde SpriteRenderer arıyoruz
            var firstRenderer = GetComponentInChildren<SpriteRenderer>();
            if (firstRenderer != null) mySprite = firstRenderer.sprite;

            // SENİN BLOCKGHOST SINIFINI ÇAĞIRIYORUZ
            // Show(BlockData data, Vector2Int cell, GridManager grid, BlockColorType colorType, Sprite visualSprite)
            _ghost.Show(_currentData, currentGridPos, _grid, _myColor, mySprite);
        }
        else
        {
            // Sığmıyorsa hayaleti gizle
            _ghost.Clear();
        }

        // 4. Bırakma İşlemi
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            
            // Hayaletleri temizle
            _ghost.Clear();
            //if(BlockSpawner.Instance) BlockSpawner.Instance.HideGhost();

            if (canPlace)
            {
                // BAŞARILI
                foreach (var c in _spawnedCells) c.OnDrop();
                Sound.Play("Drop");
                // MÜHÜRLEME (Veriyi kaydet)
                _grid.ConfirmPlacement(_shapeData, currentGridPos.x, currentGridPos.y, _myColor);
                
                if(BlockSpawner.Instance) BlockSpawner.Instance.OnBlockPlaced(this);
        
                transform.DOKill(); 
                Destroy(gameObject); 
            }
            else
            {
                // BAŞARISIZ
                foreach (var c in _spawnedCells) { c.OnDrop(); c.OnIdle(); }
                
                transform.DOMove(_startPosition, moveDuration).SetEase(Ease.OutBack);
                transform.DOScale(_slotScale, moveDuration).SetEase(Ease.OutBack);
            }
        }
    }

    private Vector2Int CalculateGridPosition()
    {
        float s = _grid.cellSize;
        Vector3 centerOffset = new Vector3((_currentData.Width * s) / 2f, (_currentData.Height * s) / 2f, 0);
        Vector3 origin = transform.position - centerOffset;
        Vector3 snapFix = new Vector3(s / 2f, s / 2f, 0);
        return _grid.WorldToCell(origin + snapFix);
    }
}