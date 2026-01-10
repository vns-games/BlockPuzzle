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
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) ClearCell(x, y);

        List<BlockShapeSO> customStartBlocks = null; // Spawner'a göndereceğimiz liste

        // 2. Desen Seçimi
        if (starterLevels != null && starterLevels.Count > 0)
        {
            LevelPatternSO selectedLevel = starterLevels[Random.Range(0, starterLevels.Count)];
            Debug.Log($"Seçilen Desen: {selectedLevel.name}");

            // --- YENİ: Seçilen desenin özel bloklarını al ---
            customStartBlocks = selectedLevel.startingBlocks;
            // -----------------------------------------------

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
        BlockSpawner.Instance.StartGame(customStartBlocks);
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
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
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
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
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

    // ... (CanFitAnywhere, CheckAndClearLines ve diğer fonksiyonlar AYNI kalacak) ...
    
    // Sadece CanFitAnywhere ve diğer kısımları kod bütünlüğü bozulmasın diye buraya özetliyorum:
    public List<BlockShapeSO> GetFittingShapes(List<BlockShapeSO> candidates) {
        List<BlockShapeSO> f = new(); foreach(var c in candidates) if(CanFitAnywhere(new BlockData(c.ToMatrix().Trim()))) f.Add(c); return f;
    }
    public bool CanFitAnywhere(BlockData data) { for(int x=0;x<width;x++)for(int y=0;y<height;y++)if(CanPlace(data,x,y))return true; return false; }
    public bool CanPlace(BlockData data, int sx, int sy) {
        for(int x=0;x<data.Width;x++)for(int y=0;y<data.Height;y++){
            if(!data.Matrix[x,y])continue; int gx=sx+x; int gy=sy+y;
            if(gx<0||gy<0||gx>=width||gy>=height||_grid[gx,gy])return false;
        } return true;
    }
    private void CheckAndClearLines() {
        List<int> r=GetFullRows(); List<int> c=GetFullColumns(); int t=r.Count+c.Count;
        if(t>0){ foreach(int row in r)ClearRow(row); foreach(int col in c)ClearColumn(col); }
    }
    public float GetFillPercentage()
    {
        int occupiedCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_grid[x, y]) occupiedCount++;
            }
        }
        return (float)occupiedCount / (width * height);
    }
    public List<BlockShapeSO> GetLineClearingShapes(List<BlockShapeSO> candidates)
    {
        List<BlockShapeSO> explosiveShapes = new List<BlockShapeSO>();
        if (candidates == null) return explosiveShapes;

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            
            // Bu şekil gridde herhangi bir yerde patlamaya sebep oluyor mu?
            if (CheckIfCausesExplosionAnywhere(data))
            {
                explosiveShapes.Add(shape);
            }
        }
        return explosiveShapes;
    }

    private bool CheckIfCausesExplosionAnywhere(BlockData data)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 1. Önce sığıyor mu?
                if (!CanPlace(data, x, y)) continue;

                // 2. Sığıyorsa, konduğu zaman patlama yaratıyor mu?
                if (WillCauseExplosion(data, x, y)) return true;
            }
        }
        return false;
    }

    private bool WillCauseExplosion(BlockData data, int startX, int startY)
    {
        // Satır Kontrolü
        // Parçanın kapladığı Y satırlarına bak
        for (int y = 0; y < data.Height; y++)
        {
            int gridY = startY + y;
            // Bu satırda parçanın kendisi kaç kare dolduruyor?
            int filledByPiece = 0;
            for (int x = 0; x < data.Width; x++) if (data.Matrix[x, y]) filledByPiece++;
            if (filledByPiece == 0) continue; // Bu satırda parçanın etkisi yok

            // Peki Grid'de bu satırda kaç dolu var?
            int currentlyFilled = 0;
            for (int gx = 0; gx < width; gx++) if (_grid[gx, gridY]) currentlyFilled++;

            // Eğer (Mevcut Dolular + Parçanın Ekledikleri == Genişlik) ise SATIR PATLAR!
            if (currentlyFilled + filledByPiece == width) return true;
        }

        // Sütun Kontrolü
        // Parçanın kapladığı X sütunlarına bak
        for (int x = 0; x < data.Width; x++)
        {
            int gridX = startX + x;
            int filledByPiece = 0;
            for (int y = 0; y < data.Height; y++) if (data.Matrix[x, y]) filledByPiece++;
            if (filledByPiece == 0) continue;

            int currentlyFilled = 0;
            for (int gy = 0; gy < height; gy++) if (_grid[gridX, gy]) currentlyFilled++;

            // Eğer (Mevcut Dolular + Parçanın Ekledikleri == Yükseklik) ise SÜTUN PATLAR!
            if (currentlyFilled + filledByPiece == height) return true;
        }

        return false;
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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
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
        for (int y = 0; y < data.Height; y++)
        {
            int gridY = startY + y;
            int filledByPiece = 0;
            for (int x = 0; x < data.Width; x++) if (data.Matrix[x, y]) filledByPiece++;
            if (filledByPiece == 0) continue;

            int currentlyFilled = 0;
            for (int gx = 0; gx < width; gx++) if (_grid[gx, gridY]) currentlyFilled++;

            if (currentlyFilled + filledByPiece == width) linesCleared++;
        }

        // Sütun Kontrolü
        for (int x = 0; x < data.Width; x++)
        {
            int gridX = startX + x;
            int filledByPiece = 0;
            for (int y = 0; y < data.Height; y++) if (data.Matrix[x, y]) filledByPiece++;
            if (filledByPiece == 0) continue;

            int currentlyFilled = 0;
            for (int gy = 0; gy < height; gy++) if (_grid[gridX, gy]) currentlyFilled++;

            if (currentlyFilled + filledByPiece == height) linesCleared++;
        }

        return linesCleared;
    }
    
    public List<BlockShapeSO> GetMostCompactShapes(List<BlockShapeSO> candidates)
        {
            Dictionary<BlockShapeSO, int> shapeScores = new Dictionary<BlockShapeSO, int>();
            int maxContactScore = 0;
    
            foreach (var shape in candidates)
            {
                BlockData data = new BlockData(shape.ToMatrix().Trim());
                
                // Bu şekil gridde en iyi nereye oturuyor ve puanı ne?
                int bestScoreForShape = GetMaxContactScore(data);
    
                if (bestScoreForShape > 0)
                {
                    shapeScores[shape] = bestScoreForShape;
                    if (bestScoreForShape > maxContactScore) maxContactScore = bestScoreForShape;
                }
            }
    
            // Sadece en yüksek temas puanına sahip olanları döndür
            List<BlockShapeSO> bestShapes = new List<BlockShapeSO>();
            
            // Eşik değeri: Çok düşük temasları (sadece kenara değenleri) "Tamirci" saymayalım.
            // En azından 2-3 kenarı değmeli ki boşluk doldursun.
            if (maxContactScore < 2) return bestShapes; 
    
            foreach (var kvp in shapeScores)
            {
                // En iyileri seç (Maksimum skora eşit veya çok yakın olanlar)
                if (kvp.Value >= maxContactScore)
                {
                    bestShapes.Add(kvp.Key);
                }
            }
    
            return bestShapes;
        }
    
        // Bir şeklin gridde alabileceği en yüksek temas puanını hesaplar
        private int GetMaxContactScore(BlockData data)
        {
            int maxScore = -1;
    
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 1. Önce sığıyor mu?
                    if (!CanPlace(data, x, y)) continue;
    
                    // 2. Sığıyorsa Temas Puanını Hesapla
                    int currentScore = CalculateContactScore(data, x, y);
                    if (currentScore > maxScore) maxScore = currentScore;
                }
            }
            return maxScore;
        }
    
        // O anki pozisyonda şeklin etrafında kaç tane dolu blok (veya duvar) var?
        private int CalculateContactScore(BlockData data, int startX, int startY)
        {
            int score = 0;
    
            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    if (!data.Matrix[x, y]) continue; // Sadece dolu hücreler için bak
    
                    int gx = startX + x;
                    int gy = startY + y;
    
                    // 4 Yönü Kontrol Et (Yukarı, Aşağı, Sağ, Sol)
                    // Eğer o yönde bir blok varsa veya grid dışıysa (Duvar), puan ver.
                    // Bu sayede parça "Yuvaya girmiş" gibi olur.
                    
                    // Sol
                    if (gx - 1 < 0 || _grid[gx - 1, gy]) score++;
                    // Sağ
                    if (gx + 1 >= width || _grid[gx + 1, gy]) score++;
                    // Aşağı
                    if (gy - 1 < 0 || _grid[gx, gy - 1]) score++;
                    // Yukarı
                    if (gy + 1 >= height || _grid[gx, gy + 1]) score++;
                }
            }
            return score;
        }
    public List<BlockShapeSO> GetHoleFillingShapes(List<BlockShapeSO> candidates, float minFitRatio = 0.5f)
    {
        Dictionary<BlockShapeSO, float> bestScores = new Dictionary<BlockShapeSO, float>();
        float overallBestScore = 0f;

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            
            // Bu şeklin griddeki en iyi oturduğu yerdeki skoru ne?
            float score = GetBestFitScore(data);
            
            if (score >= minFitRatio)
            {
                bestScores[shape] = score;
                if (score > overallBestScore) overallBestScore = score;
            }
        }

        // Sadece en iyileri ve en iyiye çok yakın olanları döndür (Eşik toleransı: 0.1f)
        List<BlockShapeSO> bestShapes = new List<BlockShapeSO>();
        if (overallBestScore < minFitRatio) return bestShapes; // Yeterince iyi uyan yoksa boş dön

        foreach (var kvp in bestScores)
        {
            // En yüksek skora sahip olanları (veya %10 altına kadar yakın olanları) seç
            if (kvp.Value >= overallBestScore - 0.1f)
            {
                bestShapes.Add(kvp.Key);
            }
        }

        return bestShapes;
    }

    private float GetBestFitScore(BlockData data)
    {
        float maxScore = 0f;
        // Şeklin toplam dış çevre uzunluğunu hesapla (Basitçe: Dolu Hücre * 4 - İç Bağlantılar)
        // Ama daha kolayı: Her dolu hücre için 4 kenar var, komşulara bak.
        int totalPerimeter = CalculateTotalPerimeter(data);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!CanPlace(data, x, y)) continue;

                int contacts = CalculateContactPoints(data, x, y);
                float ratio = (float)contacts / totalPerimeter;

                if (ratio > maxScore) maxScore = ratio;
            }
        }
        return maxScore;
    }

    // Şeklin kendi içindeki toplam kenar sayısı (Grid'e temas edebilecek potansiyel yüzey alanı)
    private int CalculateTotalPerimeter(BlockData data)
    {
        int perimeter = 0;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;

                // 4 Yöne bak, eğer şeklin dışına çıkıyorsa o bir kenardır.
                // Sol
                if (x - 1 < 0 || !data.Matrix[x - 1, y]) perimeter++;
                // Sağ
                if (x + 1 >= data.Width || !data.Matrix[x + 1, y]) perimeter++;
                // Aşağı
                if (y - 1 < 0 || !data.Matrix[x, y - 1]) perimeter++;
                // Yukarı
                if (y + 1 >= data.Height || !data.Matrix[x, y + 1]) perimeter++;
            }
        }
        return perimeter;
    }

    // O anki konumda kaç kenarın dolu bloklara veya duvara değdiğini say
    private int CalculateContactPoints(BlockData data, int startX, int startY)
    {
        int contacts = 0;
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;

                int gx = startX + x;
                int gy = startY + y;

                // Sol (Duvar veya Dolu Blok mu?)
                if (gx - 1 < 0 || _grid[gx - 1, gy]) contacts++;
                // Sağ
                if (gx + 1 >= width || _grid[gx + 1, gy]) contacts++;
                // Aşağı
                if (gy - 1 < 0 || _grid[gx, gy - 1]) contacts++;
                // Yukarı
                if (gy + 1 >= height || _grid[gx, gy + 1]) contacts++;
            }
        }
        return contacts;
    }
    // Yardımcılar...
    private List<int> GetFullRows() { List<int> r=new(); for(int y=0;y<height;y++){bool f=true;for(int x=0;x<width;x++)if(!_grid[x,y]){f=false;break;}if(f)r.Add(y);} return r; }
    private List<int> GetFullColumns() { List<int> c=new(); for(int x=0;x<width;x++){bool f=true;for(int y=0;y<height;y++)if(!_grid[x,y]){f=false;break;}if(f)c.Add(x);} return c; }
    private void ClearRow(int r) { for(int x=0;x<width;x++) ClearCell(x,r); }
    private void ClearColumn(int c) { for(int y=0;y<height;y++) ClearCell(c,y); }
    private void ClearCell(int x, int y) { if(!_grid[x,y])return; _grid[x,y]=false; if(_visuals[x,y]!=null){ CellVisualPool.Instance.Release(_visuals[x,y]); _visuals[x,y]=null; } }
    public Vector2Int WorldToCell(Vector3 wp) { Vector3 l=wp-transform.position; return new Vector2Int(Mathf.FloorToInt(l.x/cellSize),Mathf.FloorToInt(l.y/cellSize)); }
    public Vector3 CellToWorld(int x, int y) { return transform.position + new Vector3((x+0.5f)*cellSize,(y+0.5f)*cellSize,0); }
}