using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public enum SpawnerMode
{
    None,
    Relax,
    WarmUp,
    Critical,
    SmartHelp,
    SkillBased
}

public class BlockSpawner : Singleton<BlockSpawner>
{
    [Header("Shapes")]
    public List<BlockShapeSO> allShapes, easyShapes, bigShapes, cleanShapes;

    [Header("Refs")]
    public DraggableBlock blockPrefab;
    public Transform[] slots;

    [Header("Settings")]
    public float criticalThreshold = 0.85f;

    [Header("Dynamic Difficulty")]
    public Vector2 startThresholdRange = new Vector2(0.85f, 0.95f);
    public float minThreshold = 0.45f;
    public Vector2Int warmUpMovesRange = new Vector2Int(20, 40);

    // STATE Variables
    private int _totalMovesPlayed = 0;
    private int _targetWarmUpMoves;
    private float _currentStartThreshold;

    private SpawnerMode _currentMode = SpawnerMode.None;
    private List<DraggableBlock> _activeBlocks = new();

    [Header("Reward Settings")]
    public int rewardMovesPerLine = 2;

    public void StartGame()
    {
        _currentMode = SpawnerMode.None;
        _totalMovesPlayed = 0;
        _currentStartThreshold = Random.Range(startThresholdRange.x, startThresholdRange.y);
        _targetWarmUpMoves = Random.Range(warmUpMovesRange.x, warmUpMovesRange.y);
        Debug.Log($"<color=cyan>[SESSION]</color> Move Target: {_targetWarmUpMoves} | Start Threshold: {_currentStartThreshold:F2}");

        // BAŞLANGIÇ KONTROLÜ: Yatay parça var mı diye bakalım
        CheckForHorizontalBlock();

        SpawnSet();
    }

    // DEBUG İÇİN: Listende gerçekten yatay parça var mı?
    private void CheckForHorizontalBlock()
    {
        bool found = false;
        foreach (var shape in allShapes)
        {
            var m = shape.ToMatrix().Trim();
            if (m.GetLength(0) == 3 && m.GetLength(1) == 2) // 3 Genişlik, 2 Yükseklik
            {
                Debug.Log($"<color=green>[DATA CHECK] Yatay 3x2 Parça Listede Var: {shape.name}</color>");
                found = true;
            }
        }
        if (!found) Debug.LogError("<color=red>[DATA CHECK] DİKKAT! Listede 3x2 (Yatay) parça bulunamadı! Incredible çalışmaz.</color>");
    }

    public void ActivateReviveMode()
    {
        int extraMoves = 20;
        _targetWarmUpMoves = _totalMovesPlayed + extraMoves;
        Debug.Log($"<color=green><b>[REVIVE] {extraMoves} Extra Moves Added!</b></color>");
        SpawnReviveBlocks();
    }

    private float GetDynamicThreshold()
    {
        if (_totalMovesPlayed >= _targetWarmUpMoves) return minThreshold;
        float progress = (float)_totalMovesPlayed / (float)_targetWarmUpMoves;
        return Mathf.Lerp(_currentStartThreshold, minThreshold, progress);
    }

    private SpawnerMode DetermineMode(float fill)
    {
        if (fill < 0.15f) return SpawnerMode.Relax;
        if (fill > criticalThreshold) return SpawnerMode.Critical;
        if (_totalMovesPlayed < _targetWarmUpMoves) return SpawnerMode.WarmUp;
        if (fill > 0.5f) return SpawnerMode.SmartHelp;
        return SpawnerMode.SkillBased;
    }

    private List<BlockShapeSO> GetPoolForMode(SpawnerMode mode, Grid grid)
    {
        switch(mode)
        {
            // RELAX: Stres yok. Tüm şekiller gelebilir ama "Dolgun" (Mass >= 3) olsunlar.
            // 1x1 gelip keyif kaçırmasın.
            case SpawnerMode.Relax: 
                return ShapeFinder.GetSatisfyingFits(grid, allShapes);

            // WARMUP: Isınma turu. Sadece "Temiz/Düzgün" şekiller (cleanShapes) ve "Dolgun" olanlar.
            // Oyuncuya "Güzel başladık" hissi vermek için.
            case SpawnerMode.WarmUp: 
                return ShapeFinder.GetSatisfyingFits(grid, cleanShapes);

            // CRITICAL: Can pazarı! Sadece sığması en kolay (easyShapes) parçalar.
            // Burada 1x1 gelmesi serbesttir (Can simidi niyetine).
            case SpawnerMode.Critical: 
                return ShapeFinder.GetFits(grid, easyShapes);

            // SMART HELP: Aradaki delikleri "Cuk" diye kapatacak parçalar.
            case SpawnerMode.SmartHelp: 
                return ShapeFinder.GetHoleFillers(grid, allShapes, 0.7f);

            // DEFAULT: Sığan ne varsa getir.
            default: 
                return ShapeFinder.GetFits(grid, allShapes);
        }
    }

    private List<BlockShapeSO> GetWarmUpPool(Grid grid)
    {
        List<BlockShapeSO> allCandidates = new List<BlockShapeSO>(allShapes);

        var megaKillers = ShapeFinder.GetMegaKillers(grid, allCandidates);
        if (megaKillers.Count > 0)
        {
            Debug.Log($"<color=orange>[PROFILE]</color> Strategy: <b>MEGA KILLER</b> (Count: {megaKillers.Count})");
            return megaKillers;
        }

        float dynamicThreshold = GetDynamicThreshold();
        var perfectFits = ShapeFinder.GetLargePerfectFits(grid, allCandidates, dynamicThreshold);
        if (perfectFits.Count > 0)
        {
            Debug.Log($"<color=cyan>[PROFILE]</color> Strategy: <b>PERFECT FIT</b> (Threshold: {dynamicThreshold:F2})");
            return perfectFits;
        }

        var keys = ShapeFinder.GetHoleFillers(grid, allCandidates, dynamicThreshold - 0.15f);
        if (keys.Count > 0)
        {
            Debug.Log($"<color=magenta>[PROFILE]</color> Strategy: <b>HOLE FILLER</b> (Count: {keys.Count})");
            return keys;
        }

        var cleanKillers = ShapeFinder.GetCleanKillers(grid, allCandidates);
        if (cleanKillers.Count > 0)
        {
            Debug.Log($"<color=yellow>[PROFILE]</color> Strategy: <b>CLEAN KILLER</b>");
            return cleanKillers;
        }

        var fits = ShapeFinder.GetFits(grid, allCandidates);
        if (fits.Count > 0)
        {
            Debug.Log($"<color=white>[PROFILE]</color> Strategy: <b>STANDARD FIT</b>");
            return fits;
        }

        Debug.Log($"<color=red>[PROFILE]</color> Strategy: <b>EMERGENCY (Easy Shapes)</b>");
        return ShapeFinder.GetFits(grid, easyShapes);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for(int i = 0; i < list.Count; i++)
        {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private List<BlockShapeSO> GetRelaxPool(Grid grid)
    {
        var p = new List<BlockShapeSO>(bigShapes);
        p.AddRange(cleanShapes);
        var f = ShapeFinder.GetFits(grid, p);
        return f.Count > 0 ? f : ShapeFinder.GetFits(grid, allShapes);
    }

    public void SpawnReviveBlocks()
    {
        ClearActiveBlocks();
        var saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes);

        List<BlockShapeSO> batch = new List<BlockShapeSO>();
        for(int i = 0; i < slots.Length; i++)
            if (saviors.Count > 0)
                batch.Add(saviors[Random.Range(0, saviors.Count)]);

        for(int i = 0; i < batch.Count; i++)
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(batch[i]);
            _activeBlocks.Add(b);
        }
    }

    public BlockShapeSO FindGuaranteedFullClearBlock()
    {
        var grid = GridManager.Instance.LevelGrid;
        int w = grid.Width;
        int h = grid.Height;

        // Adayları ve puanlarını tutacak yapı
        BlockShapeSO bestCandidate = null;
        int maxCellCount = -1; // En çok dolu hücresi olanı tutacağız

        foreach (var blockShape in allShapes)
        {
            var rawMatrix = blockShape.ToMatrix().Trim();
            BlockData blockData = new BlockData(rawMatrix);

            // 1. ADIM: Bu parçanın kaç hücresi dolu? (Density Check)
            int currentCellCount = 0;
            for(int i = 0; i < blockData.Width; i++)
                for(int j = 0; j < blockData.Height; j++)
                    if (blockData.Matrix[i, j])
                        currentCellCount++;

            // 1x1 BLOK ENGELİ
            if (currentCellCount <= 1) continue;

            // Eğer bu parça, şu anki "En İyi Aday"dan daha az hücreye sahipse
            // ve biz zaten bir aday bulduysak, bunu simüle etmeye bile gerek yok.
            // (Amaç en dolu parçayı bulmak)
            // NOT: İlk başta aday yokken (maxCellCount -1) her türlü deneriz.
            bool worthChecking = (bestCandidate == null) || (currentCellCount > maxCellCount);

            if (!worthChecking) continue;

            // 2. ADIM: Simülasyon
            bool fitsAndClears = false;

            // Parçayı her noktada dene
            for(int x = 0; x < w; x++)
            {
                for(int y = 0; y < h; y++)
                {
                    if (CanSimulatePlace(grid, blockData, x, y))
                    {
                        if (SimulateMoveAndCheckFullClear(grid, blockData, x, y))
                        {
                            fitsAndClears = true;
                            goto EndSimulation; // İç içe döngüden çık
                        }
                    }
                }
            }
            EndSimulation: ;

            // 3. ADIM: Eğer işe yarıyorsa ve daha "dolu" bir parçaysa, yeni kral bu!
            if (fitsAndClears)
            {
                if (currentCellCount > maxCellCount)
                {
                    maxCellCount = currentCellCount;
                    bestCandidate = blockShape;

                    // İsteğe bağlı: Eğer favori parçan buysa direkt döndür
                    if (blockShape.name == "2x3_1")
                    {
                        Debug.Log($"<color=green>[BINGO!]</color> Favori parça (2x3_1) bulundu ve full dolu!");
                        return blockShape;
                    }
                }
            }
        }

        if (bestCandidate != null)
        {
            Debug.Log($"<color=green>[SONUÇ]</color> En iyi parça seçildi: {bestCandidate.name} (Dolu Hücre: {maxCellCount})");
            return bestCandidate;
        }

        return null;
    }
    private bool CanSimulatePlace(Grid grid, BlockData data, int gx, int gy)
    {
        if (gx + data.Width > grid.Width || gy + data.Height > grid.Height) return false;

        for(int x = 0; x < data.Width; x++)
        {
            for(int y = 0; y < data.Height; y++)
            {
                if (data.Matrix[x, y] && grid.Cells[gx + x, gy + y])
                    return false;
            }
        }
        return true;
    }

    private bool SimulateMoveAndCheckFullClear(Grid originalGrid, BlockData data, int gx, int gy)
    {
        bool[,] simCells = (bool[,])originalGrid.Cells.Clone();
        int w = originalGrid.Width;
        int h = originalGrid.Height;

        // Place
        for(int x = 0; x < data.Width; x++)
            for(int y = 0; y < data.Height; y++)
                if (data.Matrix[x, y])
                    simCells[gx + x, gy + y] = true;

        // Identify Lines (Hem Satır Hem Sütun)
        List<int> rowsToClear = new List<int>();
        List<int> colsToClear = new List<int>();

        // Satır Kontrol
        for(int y = 0; y < h; y++)
        {
            bool full = true;
            for(int x = 0; x < w; x++)
            {
                if (!simCells[x, y])
                {
                    full = false;
                    break;
                }
            }
            if (full) rowsToClear.Add(y);
        }

        // Sütun Kontrol (Block Puzzle Mantığı)
        for(int x = 0; x < w; x++)
        {
            bool full = true;
            for(int y = 0; y < h; y++)
            {
                if (!simCells[x, y])
                {
                    full = false;
                    break;
                }
            }
            if (full) colsToClear.Add(x);
        }

        // Clear
        foreach (int r in rowsToClear)
            for(int x = 0; x < w; x++)
                simCells[x, r] = false;
        foreach (int c in colsToClear)
            for(int y = 0; y < h; y++)
                simCells[c, y] = false;

        // Check Empty
        for(int x = 0; x < w; x++)
            for(int y = 0; y < h; y++)
                if (simCells[x, y])
                    return false;

        return true;
    }

    public void SpawnSet()
    {
        _totalMovesPlayed++;

        // 1. Temizlik
        foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        Grid grid = GridManager.Instance.LevelGrid;
        
        // --------------------------------------------------------------------
        // ADIM 1: MUTLAK ÖNCELİK (INCREDIBLE SAVIOR)
        // --------------------------------------------------------------------
        BlockShapeSO rescueBlock = FindGuaranteedFullClearBlock();

        List<BlockShapeSO> primaryPool = null; // Başlangıçta null
        List<BlockShapeSO> secondaryPool;

        if (rescueBlock != null)
        {
            Debug.Log($"<color=green>[PRIORITY]</color> INCREDIBLE SAVIOR DEVREDE!");
            primaryPool = ShapeFinder.GetFits(grid, cleanShapes); // Yanına temiz parçalar ver
            if (primaryPool.Count == 0) primaryPool = ShapeFinder.GetFits(grid, allShapes);
            _currentMode = SpawnerMode.SkillBased;
        }
        else
        {
            // Savior yoksa, mod hesapla
            float fill = grid.GetFillPercentage();
            SpawnerMode calculatedMode = DetermineMode(fill);

            // --------------------------------------------------------------------
            // ADIM 2: KRİTİK MOD İSTİSNASI (MEGA KILL FIRSATI)
            // Eğer mod "Critical" veya "Danger" ise ve tahtada Mega Kill şansı varsa,
            // oyuncuyu küçük taşlarla sıkmak yerine ona o şansı ver!
            // --------------------------------------------------------------------
            if (calculatedMode == SpawnerMode.Critical)
            {
                // Mega Kill yapabilecek bir parça var mı?
                BlockShapeSO megaKiller = ShapeFinder.FindPotentialMegaKiller(grid, allShapes);

                if (megaKiller != null)
                {
                    Debug.Log($"<color=orange>[OPPORTUNITY]</color> Kritik Moddayız ama MEGA KILL şansı var! ({megaKiller.name})");
                    
                    // Modu geçici olarak SkillBased veya Relax yapalım ki büyük taş gelebilsin
                    // Veya direkt bu taşı havuza zorla ekleyebiliriz.
                    
                    // Yöntem A: Modu Yükselt (Daha riskli ama heyecanlı)
                    // calculatedMode = SpawnerMode.SkillBased; 

                    // Yöntem B: Taşı Rezerve Et (Daha güvenli)
                    // Rescue block mantığı gibi bu taşı batch[0]'a koyacağız.
                    rescueBlock = megaKiller; 
                }
            }

            Debug.Log($"<color=yellow>[SPAWNER]</color> Mode: <b>{calculatedMode}</b> | Fill: %{fill*100:F1}");
            _currentMode = calculatedMode;

            // Havuzu şimdi oluştur (Eğer yukarıda atama yapmadıysak)
            if (primaryPool == null)
            {
                primaryPool = GetPoolForMode(calculatedMode, grid);
            }
        }

        // ====================================================================
        // ADIM 3: BATCH OLUŞTURMA
        // ====================================================================
        secondaryPool = ShapeFinder.GetFits(grid, allShapes);
        
        // Eğer primaryPool boş geldiyse (bazen olur), secondary'den doldur
        if (primaryPool == null || primaryPool.Count == 0) primaryPool = new List<BlockShapeSO>(secondaryPool);

        List<BlockShapeSO> batch = GenerateUniqueBatch(grid, primaryPool, secondaryPool, slots.Length);

        // ====================================================================
        // ADIM 4: KURTARICI VEYA MEGA KILLER ENJEKSİYONU
        // ====================================================================
        if (rescueBlock != null)
        {
            // Eğer yukarıda Incredible veya Mega Killer bulduysak, ilk sıraya koy.
            batch[0] = rescueBlock;
        }

        // Spawn...
        for (int i = 0; i < batch.Count; i++)
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(batch[i]);
            _activeBlocks.Add(b);
        }

        CheckGameOver();
    }

    private List<BlockShapeSO> GenerateUniqueBatch(Grid grid, List<BlockShapeSO> primary, List<BlockShapeSO> secondary, int count)
    {
        HashSet<BlockShapeSO> selectedSet = new HashSet<BlockShapeSO>();
        List<BlockShapeSO> finalBatch = new List<BlockShapeSO>();

        void TryFillFrom(List<BlockShapeSO> sourceList)
        {
            if (sourceList == null || sourceList.Count == 0) return;
            if (finalBatch.Count >= count) return;

            List<BlockShapeSO> shuffled = new List<BlockShapeSO>(sourceList);
            ShuffleList(shuffled);

            foreach (var shape in shuffled)
            {
                if (finalBatch.Count >= count) break;
                if (selectedSet.Contains(shape)) continue;
                if (GridManager.Instance.CanFitAnywhere(new BlockData(shape.ToMatrix().Trim())))
                {
                    selectedSet.Add(shape);
                    finalBatch.Add(shape);
                }
            }
        }

        TryFillFrom(primary);
        TryFillFrom(secondary);

        if (finalBatch.Count < count)
        {
            var allFits = ShapeFinder.GetFits(grid, allShapes);
            TryFillFrom(allFits);
        }

        if (finalBatch.Count < count)
        {
            var easyFits = ShapeFinder.GetFits(grid, easyShapes);
            while(finalBatch.Count < count && easyFits.Count > 0)
            {
                var s = easyFits[Random.Range(0, easyFits.Count)];
                finalBatch.Add(s);
            }
        }

        return finalBatch;
    }

    public void OnBlockPlaced(DraggableBlock b)
    {
        _activeBlocks.Remove(b);

        if (_activeBlocks.Count == 0)
        {
            SpawnSet();
        }
        else
        {
            StartCoroutine(CheckGameOverRoutine());
        }
    }

    private IEnumerator CheckGameOverRoutine()
    {
        yield return new WaitForEndOfFrame();
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (_activeBlocks.Count == 0) return;

        foreach (var b in _activeBlocks)
        {
            if (GridManager.Instance.CanFitAnywhere(b.GetData())) return;
        }

        Debug.Log("GAME OVER: No moves left!");
        GameManager.Instance.TriggerGameOver();
    }
    public void OnLinesCleared(int linesCount)
    {
        int bonusMoves = linesCount * rewardMovesPerLine;
        _targetWarmUpMoves += bonusMoves;
    }
    private void ClearActiveBlocks()
    {
        foreach (var b in _activeBlocks)
            if (b)
                Destroy(b.gameObject);
        _activeBlocks.Clear();
    }
}