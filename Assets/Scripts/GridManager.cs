using UnityEngine;
using VnS.Utility.Singleton;
using System.Collections; // IEnumerator için gerekli
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
            if (_levelGrid == null) _levelGrid = new Grid(width, height, cellSize);
            return _levelGrid;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null) visualRoot = transform;
    }

    // --- DEĞİŞİKLİK 1: Initialize artık Coroutine başlatıyor ---
    public void Initialize()
    {
        // Direkt çağırmak yerine, Coroutine başlatıyoruz.
        StartCoroutine(GenerateLevelRoutine());
    }

    // --- DEĞİŞİKLİK 2: Gecikmeli Oluşturucu ---
    private IEnumerator GenerateLevelRoutine()
    {
        Debug.Log("GRID: Unity hazırlanıyor... 1 frame bekleniyor.");

        // Unity'nin Render ve Fizik motorunun tam oturması için 1 frame bekle
        yield return null;

        // Garanti olsun diye bir frame daha bekle (Bazı ağır sahnelerde gerekir)
        yield return null;

        Debug.Log("GRID: Level oluşturuluyor...");
        GenerateInitialLevel();
    }

    public void GenerateInitialLevel()
    {
        // 1. TEMİZLİK
        StaticCellPool.ClearAllActive(LevelGrid.Visuals, width, height);

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                LevelGrid.ClearCell(x, y);

        // 2. LEVEL SEÇİMİ VE KONTROLÜ
        if (starterLevels == null || starterLevels.Count == 0)
        {
            Debug.LogError("HATA: Starter Levels listesi boş!");
            return;
        }

        LevelPatternSO selectedLevel = starterLevels[Random.Range(0, starterLevels.Count)];
        selectedLevel.ValidateArrays();

        Debug.Log($"SEÇİLEN LEVEL: {selectedLevel.name}");

        int foundBlocks = 0;

        // 3. OLUŞTURMA
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if (x < selectedLevel.width && y < selectedLevel.height)
                {
                    // Pattern verisinde "True" (Dolu) mu?
                    if (selectedLevel.Get(x, y))
                    {
                        LevelGrid.Cells[x, y] = true;
                        BlockColorType color = selectedLevel.GetColor(x, y);

                        // Görseli oluştur
                        CreateVisual(x, y, color, VisualSpawnType.None);

                        foundBlocks++;
                    }
                }
            }
        }

        Debug.Log($"LEVEL TAMAM. Toplam {foundBlocks} blok oluşturuldu.");

        // Eğer foundBlocks 0 ise, sorun ScriptableObject verisindedir (İçi boştur).
        if (foundBlocks == 0)
        {
            Debug.LogError("UYARI: Seçilen Level Pattern BOŞ! Inspector'da boyamayı unuttun mu?");
        }
    }

    public void PlacePiece(BlockData data, int gx, int gy, BlockColorType colorType)
    {
        if (ScoreManager.Instance) ScoreManager.Instance.RegisterMove();

        for(int x = 0; x < data.Width; x++)
        {
            for(int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;

                int targetX = gx + x;
                int targetY = gy + y;

                LevelGrid.Cells[targetX, targetY] = true;
                CreateVisual(targetX, targetY, colorType, VisualSpawnType.Placed);
            }
        }

        int totalBlocksPopped = LevelGrid.CheckAndClearMatches();

        if (totalBlocksPopped > 0)
        {
            // SKOR YÖNETİCİSİNE GÖNDER
            if (ScoreManager.Instance) ScoreManager.Instance.OnBlast(totalBlocksPopped);

            // ZORLUK/WARMUP AYARI
            // Spawner hala "Satır" mantığıyla çalışıyorsa tahmini satır sayısını gönder
            // Örn: 8 kare 1 satırsa -> total / 8
            if (BlockSpawner.Instance)
            {
                int estimatedLines = Mathf.Max(1, totalBlocksPopped / width);
                BlockSpawner.Instance.OnLinesCleared(estimatedLines);
            }
        }
    }

    private void CreateVisual(int x, int y, BlockColorType colorType, VisualSpawnType spawnType)
    {
        // Eski görsel temizliği
        if (LevelGrid.Visuals[x, y] != null)
        {
            VisualCell oldCell = LevelGrid.Visuals[x, y].GetComponent<VisualCell>();
            StaticCellPool.Despawn(oldCell);
            LevelGrid.Visuals[x, y] = null;
        }

        // POZİSYON HESABI
        Vector3 worldPos = CellToWorld(x, y);
        worldPos.z = -2f; // Öne al

        // OLUŞTURMA
        VisualCell cell = StaticCellPool.Spawn(worldPos, visualRoot);

        if (cell != null)
        {
            // Sigorta: VisualRoot'un Scale değeri bozuksa düzeltir
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

    // GridLogic bağlantıları...
    public List<BlockShapeSO> GetHoleFillingShapes(List<BlockShapeSO> c, float t) => ShapeFinder.GetHoleFillers(LevelGrid, c, t);
    public List<BlockShapeSO> GetGapFillingShapes(List<BlockShapeSO> c) => ShapeFinder.GetFits(LevelGrid, c);
    public bool CanPlace(BlockData d, int x, int y) => LevelGrid.CanPlace(d, x, y);
    public bool CanFitAnywhere(BlockData d) => LevelGrid.CanFitAnywhere(d);
    public float GetFillPercentage() => LevelGrid.GetFillPercentage();
    
    public void ShakeGrid(float strength)
    {
        // Eğer zaten sallanıyorsa önce durdur (DOKill), sonra tekrar salla.
        // complete: true parametresi, animasyonu anında bitiş konumuna (0,0,0) getirir, kayma yapmaz.
        visualRoot.DOKill(true);
        
        // Parametreler: Süre, Güç, Titreşim Sıklığı, Rastgelelik
        // Güç (strength): Ne kadar sert sallanacak? (0.5f hafif, 1.0f sert)
        visualRoot.DOShakePosition(0.4f, strength, 20, 90, false, true);
    }
}