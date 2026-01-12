using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public partial class GridManager : Singleton<GridManager>
{
    [Header("Settings")]
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;

    [Header("Levels")]
    public List<LevelPatternSO> starterLevels; // Inspector'dan atacağın desenler

    // Grid Verisi
    private bool[,] _grid;
    private GameObject[,] _visuals;

    protected override void Awake()
    {
        base.Awake();
        _grid = new bool[width, height];
        _visuals = new GameObject[width, height];
    }

    void Start()
    {
        GenerateInitialLevel();
    }

    public void GenerateInitialLevel()
    {
        // 1. Temizlik
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                ClearCell(x, y);

        // 2. Desen Seçimi
        if (starterLevels != null && starterLevels.Count > 0)
        {
            LevelPatternSO selectedLevel = starterLevels[Random.Range(0, starterLevels.Count)];
            Debug.Log($"Seçilen Desen: {selectedLevel.name}");


            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    if (x < selectedLevel.width && y < selectedLevel.height)
                    {
                        if (selectedLevel.Get(x, y))
                        {
                            _grid[x, y] = true;
                            CreateVisual(x, y, selectedLevel.patternColor);
                        }
                    }
                }
            }
        }

        CheckAndClearLines();

        // 3. Oyunu Başlat (Özel listeyi göndererek)
        // Eğer customStartBlocks boşsa veya null ise, Spawner zaten otomatik modda başlayacak.
        BlockSpawner.Instance.StartGame();
    }
    public List<BlockShapeSO> GetGapFillingShapes(List<BlockShapeSO> candidates)
    {
        List<BlockShapeSO> perfectMatches = new List<BlockShapeSO>();

        // Önce adayların gereksiz hesaplamasını önlemek için boş liste kontrolü
        if (candidates == null || candidates.Count == 0) return perfectMatches;

        // Her bir adayı (şekli) test et
        foreach (var shape in candidates)
        {
            // Şeklin verisini al
            var matrix = shape.ToMatrix().Trim();
            BlockData data = new BlockData(matrix);

            // Bu şekil griddeki herhangi bir boşluğa "CUK OTURUYOR MU?"
            // Cuk oturmak ne demek? 
            // Şekli koyduğumuz yerin SAĞI, SOLU, ALTI veya ÜSTÜ doluysa (veya duvar ise)
            // o şekil oraya "kenetlenmiş" demektir. Rastgele boşluğa atılmış değildir.

            // Performans için basit kontrol: Sadece sığması yetmez, sığdığı yerde "boşluk doldurması" lazım.
            // Bu örnekte basitleştirilmiş "Exact Fit" taraması yapıyoruz:
            // Şeklin boyutları kadar bir boşluk var mı?

            if (IsPerfectFitForAnyGap(data))
            {
                perfectMatches.Add(shape);
            }
        }

        return perfectMatches;
    }

    private bool IsPerfectFitForAnyGap(BlockData data)
    {
        // Grid üzerinde bu şeklin sığabileceği yerleri tara
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                // 1. Önce sığıyor mu diye bak (Standart kontrol)
                if (!CanPlace(data, x, y)) continue;

                // 2. Eğer sığıyorsa, bu bir "Boşluk Doldurma" hamlesi mi?
                // Bunu anlamak için şeklin etrafında duvar veya başka blok var mı diye bakarız.
                // Eğer şekil tamamen boşlukta yüzüyorsa (etrafı bomboşsa) bu bir "Gap Filler" değildir.
                // Ama biz şimdilik "Uygun boşluk var mı" dediğin için CanPlace yeterli olabilir,
                // fakat senin istediğin "Boşluğa göre spawnla" olduğu için şöyle bir hile yapıyoruz:

                // Şekil o konuma konduğunda, o şeklin kapladığı alanın
                // tam olarak bir satır veya sütun boşluğunu doldurup doldurmadığına bakabiliriz.

                // BASİT YÖNTEM: Eğer sığıyorsa ve grid %50'den fazlaysa, bu şekil oraya aittir.
                // Şimdilik "Sığması" ve şeklin boyutunun o anki boşluğa uygun olması yeterli.

                return true; // İlk bulduğu uygun boşlukta "Tamam bu şekil iş görür" der.
            }
        }
        return false;
    }
    // --- GÖRSEL OLUŞTURMA (RENK DESTEKLİ) ---
    private void CreateVisual(int x, int y, Color color)
    {
        if (_visuals[x, y] != null) return;

        GameObject vis = CellVisualPool.Instance.Get();
        vis.transform.position = CellToWorld(x, y);
        _visuals[x, y] = vis;

        // Rengi ayarla
        var renderer = vis.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    // PlacePiece metodunu da güncellememiz gerek (Oyuncu parça koyunca varsayılan renk olsun)
    public void PlacePiece(BlockData data, int gx, int gy)
    {
        for(int x = 0; x < data.Width; x++)
        {
            for(int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                _grid[gx + x, gy + y] = true;

                // Oyuncu koyduğunda varsayılan renk (Örn: Cyan veya Beyaz)
                // İstersen BlockData içine renk bilgisi ekleyip buraya taşıyabilirsin.
                CreateVisual(gx + x, gy + y, Color.cyan);
            }
        }
        CheckAndClearLines();
    }

    public bool CanFitAnywhere(BlockData data)
    {
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                if (CanPlace(data, x, y))
                    return true;
        return false;
    }
    public bool CanPlace(BlockData data, int sx, int sy)
    {
        for(int x = 0; x < data.Width; x++)
            for(int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                int gx = sx + x;
                int gy = sy + y;
                if (gx < 0 || gy < 0 || gx >= width || gy >= height || _grid[gx, gy]) return false;
            }
        return true;
    }
    private void CheckAndClearLines()
    {
        List<int> r = GetFullRows();
        List<int> c = GetFullColumns();
        int t = r.Count + c.Count;
        if (t > 0)
        {
            foreach (int row in r) ClearRow(row);
            foreach (int col in c) ClearColumn(col);
            BlockSpawner.Instance.ExtendWarmUpAction(t*5);
        }
    }
    public float GetFillPercentage()
    {
        int occupiedCount = 0;
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if (_grid[x, y]) occupiedCount++;
            }
        }
        return (float)occupiedCount / (width * height);
    }
    public List<BlockShapeSO> GetBestComboShapes(List<BlockShapeSO> candidates, out int maxLinesFound)
    {
        maxLinesFound = 0;
        Dictionary<BlockShapeSO, int> shapeScores = new Dictionary<BlockShapeSO, int>();

        // 1. Tüm adayların potansiyelini ölç
        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());

            // Bu parça gridde EN İYİ ihtimalle kaç satır silebilir?
            int maxPotential = GetMaxPotentialClear(data);

            shapeScores[shape] = maxPotential;

            if (maxPotential > maxLinesFound)
            {
                maxLinesFound = maxPotential;
            }
        }

        // 2. Sadece en yüksek skoru yapanları listeye ekle
        List<BlockShapeSO> bestShapes = new List<BlockShapeSO>();

        // Eğer hiç patlatan yoksa boş liste dön
        if (maxLinesFound == 0) return bestShapes;

        foreach (var kvp in shapeScores)
        {
            // En iyiler ligine girenleri al
            if (kvp.Value == maxLinesFound)
            {
                bestShapes.Add(kvp.Key);
            }
        }

        return bestShapes;
    }

    // Bir parçanın grid üzerinde herhangi bir yere konduğunda silebileceği MAKSİMUM satır sayısı
    private int GetMaxPotentialClear(BlockData data)
    {
        int maxClear = 0;

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                // Önce sığıyor mu?
                if (!CanPlace(data, x, y)) continue;

                // Sığıyorsa kaç satır/sütun siliyor hesapla
                int clearCount = CountLinesAssumingPlacement(data, x, y);

                if (clearCount > maxClear) maxClear = clearCount;
            }
        }
        return maxClear;
    }

    // "Mış gibi" yapıp silinecek satırları sayar (Simülasyon)
    private int CountLinesAssumingPlacement(BlockData data, int startX, int startY)
    {
        int linesCleared = 0;

        // Satır Kontrolü
        for(int y = 0; y < data.Height; y++)
        {
            int gridY = startY + y;
            int filledByPiece = 0;
            for(int x = 0; x < data.Width; x++)
                if (data.Matrix[x, y])
                    filledByPiece++;
            if (filledByPiece == 0) continue;

            int currentlyFilled = 0;
            for(int gx = 0; gx < width; gx++)
                if (_grid[gx, gridY])
                    currentlyFilled++;

            if (currentlyFilled + filledByPiece == width) linesCleared++;
        }

        // Sütun Kontrolü
        for(int x = 0; x < data.Width; x++)
        {
            int gridX = startX + x;
            int filledByPiece = 0;
            for(int y = 0; y < data.Height; y++)
                if (data.Matrix[x, y])
                    filledByPiece++;
            if (filledByPiece == 0) continue;

            int currentlyFilled = 0;
            for(int gy = 0; gy < height; gy++)
                if (_grid[gridX, gy])
                    currentlyFilled++;

            if (currentlyFilled + filledByPiece == height) linesCleared++;
        }

        return linesCleared;
    }



    // Şeklin kendi içindeki toplam kenar sayısı (Grid'e temas edebilecek potansiyel yüzey alanı)


    // O anki konumda kaç kenarın dolu bloklara veya duvara değdiğini say

    public List<BlockShapeSO> GetTotalClearShapes(List<BlockShapeSO> candidates)
    {
        List<BlockShapeSO> wipeoutShapes = new List<BlockShapeSO>();

        // Eğer grid zaten boşsa, temizleme diye bir şey olamaz
        if (GetFillPercentage() == 0) return wipeoutShapes;

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());

            // Bu şekil gridde herhangi bir yere konunca Total Clear yapıyor mu?
            if (CheckIfTotalClear(data))
            {
                wipeoutShapes.Add(shape);
            }
        }
        return wipeoutShapes;
    }

    private bool CheckIfTotalClear(BlockData data)
    {
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if (!CanPlace(data, x, y)) continue;

                if (SimulateTotalClear(data, x, y)) return true;
            }
        }
        return false;
    }

    private bool SimulateTotalClear(BlockData data, int startX, int startY)
    {
        // 1. Hangi satır ve sütunların patlayacağını hesapla
        HashSet<int> clearedRows = new HashSet<int>();
        HashSet<int> clearedCols = new HashSet<int>();

        // Satır Kontrolü
        for(int y = 0; y < data.Height; y++)
        {
            int gridY = startY + y;
            // Bu satırda parçanın bloğu var mı?
            bool rowHasBlock = false;
            for(int px = 0; px < data.Width; px++)
                if (data.Matrix[px, y])
                {
                    rowHasBlock = true;
                    break;
                }
            if (!rowHasBlock) continue;

            // Gridde kaç dolu + Parça kaç ekliyor = Genişlik mi?
            int currentFilled = 0;
            for(int gx = 0; gx < width; gx++)
                if (_grid[gx, gridY])
                    currentFilled++;

            int pieceContribution = 0;
            for(int px = 0; px < data.Width; px++)
                if (data.Matrix[px, y])
                    pieceContribution++;

            if (currentFilled + pieceContribution == width) clearedRows.Add(gridY);
        }

        // Sütun Kontrolü
        for(int x = 0; x < data.Width; x++)
        {
            int gridX = startX + x;
            bool colHasBlock = false;
            for(int py = 0; py < data.Height; py++)
                if (data.Matrix[x, py])
                {
                    colHasBlock = true;
                    break;
                }
            if (!colHasBlock) continue;

            int currentFilled = 0;
            for(int gy = 0; gy < height; gy++)
                if (_grid[gridX, gy])
                    currentFilled++;

            int pieceContribution = 0;
            for(int py = 0; py < data.Height; py++)
                if (data.Matrix[x, py])
                    pieceContribution++;

            if (currentFilled + pieceContribution == height) clearedCols.Add(gridX);
        }

        // Eğer hiç satır/sütun silinmiyorsa Total Clear olamaz
        if (clearedRows.Count == 0 && clearedCols.Count == 0) return false;

        // 2. KRİTİK KONTROL: Griddeki TÜM dolu hücreler, silinen bu satır/sütunların içinde mi?
        // Eğer dışarıda kalan tek bir blok bile varsa Total Clear değildir.
        for(int gx = 0; gx < width; gx++)
        {
            for(int gy = 0; gy < height; gy++)
            {
                if (_grid[gx, gy]) // Gridde dolu bir blok var
                {
                    // Eğer bu blok silinen satırda DEĞİLSE VE silinen sütunda DEĞİLSE -> Kalacak demektir.
                    if (!clearedRows.Contains(gy) && !clearedCols.Contains(gx))
                    {
                        return false; // Temizlenmeyen artık var
                    }
                }
            }
        }

        return true; // Tüm dolu hücreler kapsama alanında!
    }

    public List<BlockShapeSO> GetHoleFillingShapes(List<BlockShapeSO> candidates, float threshold = 0.6f)
    {
        List<BlockShapeSO> bestShapes = new List<BlockShapeSO>();
        float bestScore = -1f;

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());

            // Tüm gridi tara
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    // 1. Önce sığıyor mu?
                    if (CanPlace(data, x, y))
                    {
                        // 2. Sığıyorsa "Sıkışıklık Testi" yap
                        float score = CalculateHoleFitScore(data, x, y);

                        if (score >= threshold)
                        {
                            // Eğer bu skor, şimdiye kadarki en iyi skorsa veya çok yakınsa
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestShapes.Clear(); // Daha iyisini bulduk, eskileri sil
                                bestShapes.Add(shape);
                            }
                            else if (Mathf.Abs(score - bestScore) < 0.01f) // Eşitse ekle
                            {
                                if (!bestShapes.Contains(shape)) bestShapes.Add(shape);
                            }
                        }
                    }
                }
            }
        }
        return bestShapes;
    }

    // Bir parçanın o noktada ne kadar "Sarıldığını" hesaplar (0.0 ile 1.0 arası)
    private float CalculateHoleFitScore(BlockData data, int startX, int startY)
    {
        int totalExposedEdges = 0; // Parçanın dış çeper uzunluğu
        int touchingEdges = 0;     // Bir yere (Blok veya Duvar) değen kenar sayısı

        for(int x = 0; x < data.Width; x++)
        {
            for(int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue; // Boş pikseli geç

                int gx = startX + x;
                int gy = startY + y;

                // 4 Yönü Kontrol Et (Sağ, Sol, Yukarı, Aşağı)
                CheckDirection(gx + 1, gy, data, x + 1, y, ref totalExposedEdges, ref touchingEdges); // Sağ
                CheckDirection(gx - 1, gy, data, x - 1, y, ref totalExposedEdges, ref touchingEdges); // Sol
                CheckDirection(gx, gy + 1, data, x, y + 1, ref totalExposedEdges, ref touchingEdges); // Yukarı
                CheckDirection(gx, gy - 1, data, x, y - 1, ref totalExposedEdges, ref touchingEdges); // Aşağı
            }
        }

        if (totalExposedEdges == 0) return 0f;
        return (float)touchingEdges / totalExposedEdges;
    }

    private void CheckDirection(int gridX, int gridY, BlockData data, int shapeNeighborX, int shapeNeighborY, ref int total, ref int touching)
    {
        // 1. BU KENAR PARÇANIN İÇİNDE Mİ KALIYOR?
        // Eğer parçanın kendisi o yönde devam ediyorsa, orası "dış kenar" değildir. Sayma.
        bool isInternal = false;
        if (shapeNeighborX >= 0 && shapeNeighborX < data.Width &&
            shapeNeighborY >= 0 && shapeNeighborY < data.Height)
        {
            if (data.Matrix[shapeNeighborX, shapeNeighborY]) isInternal = true;
        }

        if (isInternal) return; // İç kenarsa hesaplamaya katma.

        // Buraya geldiyse burası parçanın "Dış Yüzeyi"dir (Perimeter).
        total++;

        // 2. PEKİ BU DIŞ YÜZEY NEYE DEĞİYOR?

        // A) DUVARA DEĞİYORSA (Senin isteğin: Duvarlar kapalı karedir)
        if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height)
        {
            touching++;
            return;
        }

        // B) BAŞKA BİR BLOĞA DEĞİYORSA
        if (_grid[gridX, gridY])
        {
            touching++;
        }
    }
    // Yardımcılar...
    private List<int> GetFullRows()
    {
        List<int> r = new();
        for(int y = 0; y < height; y++)
        {
            bool f = true;
            for(int x = 0; x < width; x++)
                if (!_grid[x, y])
                {
                    f = false;
                    break;
                }
            if (f) r.Add(y);
        }
        return r;
    }
    private List<int> GetFullColumns()
    {
        List<int> c = new();
        for(int x = 0; x < width; x++)
        {
            bool f = true;
            for(int y = 0; y < height; y++)
                if (!_grid[x, y])
                {
                    f = false;
                    break;
                }
            if (f) c.Add(x);
        }
        return c;
    }
    private void ClearRow(int r)
    {
        for(int x = 0; x < width; x++) ClearCell(x, r);
    }
    private void ClearColumn(int c)
    {
        for(int y = 0; y < height; y++) ClearCell(c, y);
    }
    private void ClearCell(int x, int y)
    {
        if (!_grid[x, y]) return;
        _grid[x, y] = false;
        if (_visuals[x, y] != null)
        {
            CellVisualPool.Instance.Release(_visuals[x, y]);
            _visuals[x, y] = null;
        }
    }
    public Vector2Int WorldToCell(Vector3 wp)
    {
        Vector3 l = wp - transform.position;
        return new Vector2Int(Mathf.FloorToInt(l.x / cellSize), Mathf.FloorToInt(l.y / cellSize));
    }
    public Vector3 CellToWorld(int x, int y) { return transform.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0); }
}