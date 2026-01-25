using UnityEngine;
using VnS.Utility.Singleton;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public partial class GridManager : Singleton<GridManager>
{
    [Header("Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;
    public Transform visualRoot;

    [Header("Levels")]
    public List<LevelPatternSO> starterLevels;

    private Grid _levelGrid;
    public Grid LevelGrid
    {
        get
        {
            _levelGrid ??= new Grid(width, height, cellSize);
            return _levelGrid;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null) visualRoot = transform;
    }

    public void Initialize()
    {
        StartCoroutine(GenerateLevelRoutine());
    }

    private IEnumerator GenerateLevelRoutine()
    {
        yield return null;
        yield return null;

        Debug.Log("GRID: Level oluşturuluyor...");
        GenerateInitialLevel();
        BlockSpawner.Instance.StartGame();
        ParticleManager.Instance.Initialize(width, height);
    }

    public void GenerateInitialLevel()
    {
        // 1. TEMİZLİK
        if (LevelGrid.Visuals != null)
        {
            StaticCellPool.ClearAllActive(LevelGrid.Visuals, width, height);
        }

        if (LevelGrid.Cells != null)
        {
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    LevelGrid.ClearCell(x, y);
                }
            }
        }

        // 2. OLUŞTURMA
        if (starterLevels == null || starterLevels.Count == 0)
        {
            Debug.LogError("HATA: Starter Levels listesi boş!");
            return;
        }

        LevelPatternSO selectedLevel = starterLevels[Random.Range(0, starterLevels.Count)];
        selectedLevel.ValidateArrays();

        int foundBlocks = 0;
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if (x < selectedLevel.width && y < selectedLevel.height)
                {
                    if (selectedLevel.Get(x, y))
                    {
                        LevelGrid.Cells[x, y] = true;
                        BlockColorType color = selectedLevel.GetColor(x, y);
                        CreateVisual(x, y, color, VisualSpawnType.None);
                        foundBlocks++;
                    }
                }
            }
        }

        Debug.Log($"LEVEL TAMAM. {foundBlocks} blok yerleştirildi.");
    }

    // --- KRİTİK FONKSİYON: BLOĞU MÜHÜRLEME ---
    public void ConfirmPlacement(BlockShapeSO shape, int startX, int startY, BlockColorType colorType)
    {
        // 1. VERİYİ GÜNCELLE (Hafızaya Yaz)
        // DraggableBlock'tan gelen ham veriyi kullanıyoruz
        int w = shape.width;
        int h = shape.height;
        bool[] cells = shape.cells;

        for(int x = 0; x < w; x++)
        {
            for(int y = 0; y < h; y++)
            {
                // Şeklin dolu kısımlarını grid'e işle
                if (cells[y * w + x])
                {
                    int gridX = startX + x;
                    int gridY = startY + y;

                    // Sınır kontrolü (Sigorta)
                    if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                    {
                        LevelGrid.Cells[gridX, gridY] = true;                          // HAFIZA GÜNCELLENDİ
                        CreateVisual(gridX, gridY, colorType, VisualSpawnType.Placed); // GÖRSEL OLUŞTU
                    }
                }
            }
        }

        // 2. PATLATMA KONTROLÜ
        // Yerleştirme bitti, şimdi satır/sütun oluştu mu diye bak
        CheckMatchesAndScore(shape, colorType, startX, startY);
    }

    // Match ve Skor işlemlerini buraya aldık
    // NOT: Bu metodu çağırdığın yerde (ConfirmPlacement) artık 'shape' parametresini de göndermelisin.
private void CheckMatchesAndScore(BlockShapeSO shape, BlockColorType colorType, int startX, int startY)
{
    int blocksPopped = LevelGrid.CheckAndClearMatches(out int linesCleared, colorType);

    if (blocksPopped > 0)
    {
        bool isFullClear = LevelGrid.IsGridEmpty();

        if (ScoreManager.Instance)
        {
            ScoreManager.Instance.OnBlast(blocksPopped, linesCleared, isFullClear);
        }

        if (BlockSpawner.Instance)
            BlockSpawner.Instance.OnLinesCleared(Mathf.Max(1, blocksPopped / width));

        if (ScoreManager.Instance)
        {
            int accumulator = ScoreManager.Instance.ComboAccumulator;
            int multiplier = ScoreManager.Instance.Multiplier;

            // Sadece Combo (Çarpan) varsa göster
            if (multiplier >= 1 && accumulator > 0)
            {
                // 1. İDEAL POZİSYON (Bloğun Tam Ortası)
                Vector3 rootWorldPos = CellToWorld(startX, startY);
                float halfW = (shape.width * cellSize) / 2f;
                float halfH = (shape.height * cellSize) / 2f;
                Vector3 centerPos = rootWorldPos + new Vector3(halfW, halfH, 0);

                // 2. GRID SINIRLARI VE MARGIN (Kenar Payı)
                // Grid'in başladığı yer (transform.position)
                float gridMinX = transform.position.x;
                float gridMinY = transform.position.y;
                
                // Grid'in bittiği yer (Genişlik * Hücre Boyutu)
                float gridMaxX = gridMinX + (width * cellSize);
                float gridMaxY = gridMinY + (height * cellSize);

                // Yazı kenara yapışmasın diye pay bırakıyoruz (1.5 birim idealdir)
                float margin = 1.5f; 

                // 3. CLAMP İŞLEMİ (Hapsetme)
                float clampedX = Mathf.Clamp(centerPos.x, gridMinX + margin, gridMaxX - margin);
                float clampedY = Mathf.Clamp(centerPos.y, gridMinY + margin, gridMaxY - margin);

                Vector3 finalPos = new Vector3(clampedX, clampedY, -5f); // Z'yi öne al

                // 4. GÖNDER
                ComboLayoutController.Instance.SetComboValue(multiplier, finalPos);
            }
        }
    }
    else
    {
        // Patlama olmadıysa hamle yapıldı sesi vs.
        if (ScoreManager.Instance) ScoreManager.Instance.RegisterMove();
    }
}

    private void CreateVisual(int x, int y, BlockColorType colorType, VisualSpawnType spawnType)
    {
        if (LevelGrid.Visuals[x, y] != null)
        {
            VisualCell oldCell = LevelGrid.Visuals[x, y].GetComponent<VisualCell>();
            StaticCellPool.Despawn(oldCell);
            LevelGrid.Visuals[x, y] = null;
        }

        Vector3 worldPos = CellToWorld(x, y);
        worldPos.z = -2f;

        VisualCell cell = StaticCellPool.Spawn(worldPos, visualRoot);

        if (cell != null)
        {
            if (visualRoot.localScale == Vector3.zero) visualRoot.localScale = Vector3.one;
            cell.Initialize(colorType, 10, spawnType);
            LevelGrid.Visuals[x, y] = cell.gameObject;
        }
    }

    // --- WRAPPERS ---
    public Vector2Int WorldToCell(Vector3 wp)
    {
        Vector3 l = wp - transform.position;
        return new Vector2Int(Mathf.FloorToInt(l.x / cellSize), Mathf.FloorToInt(l.y / cellSize));
    }
    public Vector3 CellToWorld(int x, int y) => transform.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);

    public bool CanPlace(BlockData d, int x, int y) => LevelGrid.CanPlace(d, x, y);
    public bool CanFitAnywhere(BlockData d) => LevelGrid.CanFitAnywhere(d);

    public void ShakeGrid(float strength)
    {
        visualRoot.DOKill(true);
        visualRoot.DOShakePosition(0.4f, strength, 20, 90, false, true);
    }

}