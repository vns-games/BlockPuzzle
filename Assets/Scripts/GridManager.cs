using UnityEngine;
using VnS.Utility.Singleton;
using System.Collections.Generic;


public partial class GridManager : Singleton<GridManager>
{
    [Header("Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;
    
    [Header("Levels")]
    public List<LevelPatternSO> starterLevels;

    // --- LAZY INITIALIZATION (RACE CONDITION FIX) ---
    private Grid _levelGrid;
    public Grid LevelGrid 
    { 
        get 
        {
            // Eğer grid henüz oluşturulmadıysa, istendiği o saniye oluşturuyoruz.
            // Böylece Awake sırası ne olursa olsun asla null gelmez.
            if (_levelGrid == null)
            {
                _levelGrid = new Grid(width, height, cellSize);
            }
            return _levelGrid;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        // Isınma turu: Grid'in oluştuğundan emin oluyoruz.
        var ensureCreated = LevelGrid; 
    }

    void Start()
    {
        GenerateInitialLevel();
    }

    public void GenerateInitialLevel()
    {
        // 1. Temizlik (GridLogic Extension Metodu)
        for(int x = 0; x < width; x++) 
            for(int y = 0; y < height; y++) 
                LevelGrid.ClearCell(x, y);

        // 2. Level Desenini Yükle
        if (starterLevels != null && starterLevels.Count > 0)
        {
            LevelPatternSO selectedLevel = starterLevels[Random.Range(0, starterLevels.Count)];
            
            // Veri dizilerinin sağlam olduğundan emin ol (Editör dışında da çalışması için)
            selectedLevel.ValidateArrays(); 

            Debug.Log($"Seçilen Desen: {selectedLevel.name}");

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    if (x < selectedLevel.width && y < selectedLevel.height)
                    {
                        if (selectedLevel.Get(x, y)) // Dolu mu?
                        {
                            LevelGrid.Cells[x, y] = true;
                            
                            // YENİ: Hücrenin kendine özel rengini al
                            BlockColorType cellSpecificColor = selectedLevel.GetColor(x, y);
                            
                            // Level Pattern olduğu için VisualSpawnType.None (Sessiz)
                            CreateVisual(x, y, cellSpecificColor, VisualSpawnType.None);
                        }
                    }
                }
            }
        }
        
        // Başlangıçta hazır patlayacak satır varsa temizle (Puan vermeden)
        LevelGrid.CheckAndClearMatches();

        // Grid hazır, Spawner başlayabilir
        BlockSpawner.Instance.StartGame();
    }

    // --- OYUNCU HAMLESİ (ON PLACED) ---
    // DraggableBlock'tan çağrılır. Rengi ve veriyi alır.
    public void PlacePiece(BlockData data, int gx, int gy, BlockColorType colorType)
    {
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                
                // Mantıksal gridi güncelle
                LevelGrid.Cells[gx + x, gy + y] = true;

                // Görseli oluştur.
                // TİP: PLACED (Oyuncu koydu, toz/sarsıntı efekti oynasın)
                CreateVisual(gx + x, gy + y, colorType, VisualSpawnType.Placed);
            }
        }

        // Patlama Kontrolü (GridLogic Extension Metodu)
        int cleared = LevelGrid.CheckAndClearMatches();
        
        // Eğer satır silindiyse WarmUp süresini uzat
        if (cleared > 0)
        {
            BlockSpawner.Instance.ExtendWarmUp(cleared * 5f);
        }
    }

    // --- GÖRSEL OLUŞTURUCU (MERKEZİ METOD) ---
    private void CreateVisual(int x, int y, BlockColorType colorType, VisualSpawnType spawnType)
    {
        // Zaten görsel varsa tekrar oluşturma
        if (LevelGrid.Visuals[x, y] != null) return;

        // Havuzdan obje çek
        GameObject visObj = CellVisualPool.Instance.Get();
        visObj.transform.position = CellToWorld(x, y);

        // VisualCell ayarlarını yap
        VisualCell cell = visObj.GetComponent<VisualCell>();
        if (cell != null)
        {
            // Tipi parametre olarak gönderiyoruz, Initialize içinde switch-case ile karar veriyor.
            // 5 sorting order gridin zemininden yüksekte durmasını sağlar.
            cell.Initialize(colorType, 5, spawnType); 
        }

        // Görseli gride kaydet
        LevelGrid.Visuals[x, y] = visObj;
    }

    // --- WRAPPERS (ShapeFinder ve GridLogic Köprüleri) ---
    // Spawner bu fonksiyonları çağırarak analiz yapar.
    
    public List<BlockShapeSO> GetBestComboShapes(List<BlockShapeSO> c, out int max) => ShapeFinder.GetComboShapes(LevelGrid, c, out max);
    public List<BlockShapeSO> GetHoleFillingShapes(List<BlockShapeSO> c, float t) => ShapeFinder.GetHoleFillers(LevelGrid, c, t);
    public List<BlockShapeSO> GetGapFillingShapes(List<BlockShapeSO> c) => ShapeFinder.GetFits(LevelGrid, c);
    
    public List<BlockShapeSO> GetTotalClearShapes(List<BlockShapeSO> c) 
    {
        // TotalClear şimdilik boş, ileride ShapeFinder'a eklenebilir.
        return new List<BlockShapeSO>(); 
    }

    // GridLogic extension metodlarına erişim
    public bool CanPlace(BlockData d, int x, int y) => LevelGrid.CanPlace(d, x, y);
    public bool CanFitAnywhere(BlockData d) => LevelGrid.CanFitAnywhere(d);
    public float GetFillPercentage() => LevelGrid.GetFillPercentage();

    // Koordinat Dönüşümleri
    public Vector2Int WorldToCell(Vector3 wp)
    {
        Vector3 l = wp - transform.position;
        return new Vector2Int(Mathf.FloorToInt(l.x / cellSize), Mathf.FloorToInt(l.y / cellSize));
    }
    public Vector3 CellToWorld(int x, int y) => transform.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
}