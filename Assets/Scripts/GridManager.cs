using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public class GridManager : Singleton<GridManager>
{
    public enum CellType { Normal, Lightning }

    [Header("Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;

    [Header("Juice Settings")]
    public Color normalColor = new Color(0, 1, 1, 1);
    public Color lightningColor = new Color(1, 0.9f, 0, 1); // Altın Sarısı
    
    public int comboThreshold = 4;        // Toplam kaç satırda bir ödül?
    public int lightningExplosionCount = 5; // Yıldırım başına kaç kare gitsin?

    // Veri
    private bool[,] _grid;
    private CellType[,] _cellTypes; 
    private GameObject[,] _visuals;
    
    // Sayaç
    private int _linesClearedProgress = 0; 

    protected override void Awake()
    {
        base.Awake();
        _grid = new bool[width, height];
        _cellTypes = new CellType[width, height]; 
        _visuals = new GameObject[width, height];
    }

    // --- STANDART METOTLAR (Place, Check vb.) ---
    
    public bool CanFitAnywhere(BlockData data)
    {
        bool[,] testMatrix = data.Matrix;
        for (int i = 0; i < 4; i++)
        {
            BlockData testData = new BlockData(testMatrix);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (CanPlace(testData, x, y)) return true;

            if (i < 3) testMatrix = testMatrix.RotateRight().Trim();
        }
        return false; 
    }

    public bool CanPlace(BlockData data, int startX, int startY)
    {
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                int gx = startX + x;
                int gy = startY + y;
                if (gx < 0 || gy < 0 || gx >= width || gy >= height) return false;
                if (_grid[gx, gy]) return false;
            }
        }
        return true;
    }

    public void PlacePiece(BlockData data, int gx, int gy)
    {
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                int cx = gx + x;
                int cy = gy + y;

                _grid[cx, cy] = true;
                _cellTypes[cx, cy] = CellType.Normal; 
                CreateVisual(cx, cy, CellType.Normal);
            }
        }
        CheckAndClearLines();
    }

    private void CreateVisual(int x, int y, CellType type)
    {
        if (_visuals[x, y] != null) return;
        GameObject vis = CellVisualPool.Instance.Get();
        vis.transform.position = CellToWorld(x, y);
        _visuals[x, y] = vis;

        var renderer = vis.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = (type == CellType.Lightning) ? lightningColor : normalColor;
    }

    // --- TEMİZLEME ve ZİNCİRLEME MANTIĞI (GÜNCELLENDİ) ---

    private void CheckAndClearLines()
    {
        List<int> fullRows = GetFullRows();
        List<int> fullCols = GetFullColumns();

        int currentClearedCount = fullRows.Count + fullCols.Count;
        if (currentClearedCount == 0) return;

        // 1. ADIM: Yıldırımları Tespit Et (HashSet ile)
        // HashSet kullanıyoruz ki aynı yıldırım hem satır hem sütunda ise 2 kere sayılmasın.
        HashSet<Vector2Int> lightningsInLines = new HashSet<Vector2Int>();

        foreach (int y in fullRows)
        {
            for (int x = 0; x < width; x++)
                if (_cellTypes[x, y] == CellType.Lightning)
                    lightningsInLines.Add(new Vector2Int(x, y));
        }

        foreach (int x in fullCols)
        {
            for (int y = 0; y < height; y++)
                if (_cellTypes[x, y] == CellType.Lightning)
                    lightningsInLines.Add(new Vector2Int(x, y));
        }

        // Toplam Patlatma Gücü Hesabı
        // Örn: 2 Yıldırım varsa -> 2 * 5 = 10 kare patlayacak
        int totalExplosionPower = lightningsInLines.Count * lightningExplosionCount;

        // 2. ADIM: Satırları Sil
        foreach (int row in fullRows) ClearRow(row);
        foreach (int col in fullCols) ClearColumn(col);

        // 3. ADIM: Patlamaları Başlat
        if (totalExplosionPower > 0)
        {
            Debug.Log($"<color=yellow>YILDIRIM AKTİF! Kaynak: {lightningsInLines.Count} adet. Hedef: {totalExplosionPower} kare.</color>");
            // Zincirleme reaksiyonu başlat
            ExplodeRandomCells(totalExplosionPower);
        }

        // 4. ADIM: Combo Sayacı
        _linesClearedProgress += currentClearedCount;
        
        if (_linesClearedProgress >= comboThreshold)
        {
            SpawnLightningBlock();
            _linesClearedProgress -= comboThreshold;
        }
    }

    // ZİNCİRLEME REAKSİYON FONKSİYONU
    private void ExplodeRandomCells(int countToExplode)
    {
        // 1. Griddeki tüm dolu hücreleri bul
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (_grid[x, y]) occupiedCells.Add(new Vector2Int(x, y));

        // Eğer patlatılacak yer yoksa dur
        if (occupiedCells.Count == 0) return;

        // 2. Liste Karıştırma (Shuffle)
        // Böylece her zaman farklı ve rastgele kareleri hedefleriz
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            Vector2Int temp = occupiedCells[i];
            int rnd = Random.Range(i, occupiedCells.Count);
            occupiedCells[i] = occupiedCells[rnd];
            occupiedCells[rnd] = temp;
        }

        // 3. Limit Belirleme (İstenen sayı veya mevcut dolu sayıdan hangisi küçükse)
        int limit = Mathf.Min(countToExplode, occupiedCells.Count);
        
        // Zincirleme reaksiyon için birikim
        int chainReactionBonus = 0;

        // 4. Patlatma Döngüsü
        for (int i = 0; i < limit; i++)
        {
            Vector2Int pos = occupiedCells[i];

            // --- KRİTİK KISIM: ZİNCİRLEME KONTROLÜ ---
            // Eğer patlattığımız bu rastgele kare de bir YILDIRIM ise?
            if (_cellTypes[pos.x, pos.y] == CellType.Lightning)
            {
                chainReactionBonus += lightningExplosionCount;
                Debug.Log($"<color=red>ZİNCİRLEME! ({pos.x},{pos.y}) de bir yıldırım daha patladı!</color>");
            }

            // Kareyi Sil
            ClearCell(pos.x, pos.y);
            
            // Görsel Efekt (Particle) çağrısı buraya eklenebilir
        }

        // 5. RECURSION (Kendini Çağırma)
        // Eğer zincirleme bonusu çıktıysa, fonksiyonu tekrar çağır
        if (chainReactionBonus > 0)
        {
            ExplodeRandomCells(chainReactionBonus);
        }
    }

    private void SpawnLightningBlock()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!_grid[x, y]) emptyCells.Add(new Vector2Int(x, y));

        if (emptyCells.Count == 0) return;

        Vector2Int target = emptyCells[Random.Range(0, emptyCells.Count)];
        _grid[target.x, target.y] = true;
        _cellTypes[target.x, target.y] = CellType.Lightning;
        CreateVisual(target.x, target.y, CellType.Lightning);
    }

    // --- TEMEL YARDIMCILAR ---
    private List<int> GetFullRows() { /* AA.txt veya önceki kod ile aynı */ 
        List<int> rows = new List<int>();
        for (int y = 0; y < height; y++) {
            bool isFull = true;
            for (int x = 0; x < width; x++) if (!_grid[x, y]) { isFull = false; break; }
            if (isFull) rows.Add(y);
        }
        return rows;
    }
    private List<int> GetFullColumns() { /* AA.txt veya önceki kod ile aynı */
        List<int> cols = new List<int>();
        for (int x = 0; x < width; x++) {
            bool isFull = true;
            for (int y = 0; y < height; y++) if (!_grid[x, y]) { isFull = false; break; }
            if (isFull) cols.Add(x);
        }
        return cols;
    }
    private void ClearRow(int rowIndex) { for (int x = 0; x < width; x++) ClearCell(x, rowIndex); }
    private void ClearColumn(int colIndex) { for (int y = 0; y < height; y++) ClearCell(colIndex, y); }
    private void ClearCell(int x, int y)
    {
        if (!_grid[x, y]) return;
        _grid[x, y] = false;
        _cellTypes[x, y] = CellType.Normal; // Tipi sıfırla
        if (_visuals[x, y] != null) {
            CellVisualPool.Instance.Release(_visuals[x, y]);
            _visuals[x, y] = null;
        }
    }

    public Vector2Int WorldToCell(Vector3 worldPos) {
        Vector3 local = worldPos - transform.position;
        return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.y / cellSize));
    }
    public Vector3 CellToWorld(int x, int y) {
        return transform.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0);
    }
    // --- EDITOR GÖRÜNÜMÜ (GIZMOS) ---

    private void OnDrawGizmos()
    {
        // Grid verisi yoksa (Editördeyken) geçici oluştur ki çizgileri görelim
        if (_grid == null || _grid.GetLength(0) != width || _grid.GetLength(1) != height)
        {
            // Sadece çizim amaçlı
        }

        // 1. Grid Çizgileri
        Gizmos.color = Color.gray;
        for(int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(x * cellSize, 0),
                transform.position + new Vector3(x * cellSize, height * cellSize)
            );
        }
        for(int y = 0; y <= height; y++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(0, y * cellSize),
                transform.position + new Vector3(width * cellSize, y * cellSize)
            );
        }

        // 2. Dolu Hücreler (Sadece Play modunda çalışır)
        if (Application.isPlaying && _grid != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Yarı saydam kırmızı
            Vector3 size = Vector3.one * (cellSize * 0.9f);
            Vector3 offset = new Vector3(cellSize / 2f, cellSize / 2f, 0);

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    if (_grid[x, y])
                    {
                        Vector3 pos = transform.position + new Vector3(x * cellSize, y * cellSize, 0) + offset;
                        Gizmos.DrawCube(pos, size);
                    }
                }
            }
        }
    }
}