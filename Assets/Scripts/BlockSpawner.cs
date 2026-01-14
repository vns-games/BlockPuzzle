using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public enum SpawnerMode
{
    None,
    Relax,
    WarmUp, // Öncelik: MegaKill -> BigFit -> CleanKill -> AnyFit
    Critical,
    SmartHelp,
    SkillBased
}

public class BlockSpawner : Singleton<BlockSpawner>
{
    [Header("Shapes")]
    public List<BlockShapeSO> allShapes, easyShapes, bigShapes, cleanShapes, cornerShapes;

    [Header("Refs")]
    public DraggableBlock blockPrefab;
    public Transform[] slots;

    [Header("Settings")]
    public float criticalThreshold = 0.85f;

    [Header("Dynamic Difficulty (Session Based)")]
    // Eşiğin başlayacağı aralık (Örn: 0.85 ile 0.95 arası rastgele bir değer seçilir)
    public Vector2 startThresholdRange = new Vector2(0.85f, 0.95f);

    // Eşiğin en son düşeceği taban değer (Oyun oturduktan sonraki zorluk)
    public float minThreshold = 0.45f;

    // Isınma sürecinin kaç hamle süreceği (Örn: 20 ile 40 hamle arası rastgele)
    public Vector2Int warmUpMovesRange = new Vector2Int(20, 40);

    // STATE (Durum Değişkenleri)
    private int _totalMovesPlayed = 0;      // Toplam oynanan el sayısı
    private int _targetWarmUpMoves;         // Bu oyun için belirlenen hedef ısınma süresi (hamle cinsinden)
    private float _currentStartThreshold;   // Bu oyun için belirlenen başlangıç zorluğu

    private SpawnerMode _currentMode = SpawnerMode.None;
    private List<DraggableBlock> _activeBlocks = new();
    [Header("Reward Settings")]
    [Tooltip("Her patlatılan satır başına zorluk ne kadar ötelenecek? (Örn: 2 hamle)")]
    public int rewardMovesPerLine = 2; // "1 ileri 2 geri" mantığı için burayı 2 yapabilirsin.
    public void StartGame()
    {
        _currentMode = SpawnerMode.None;
        _totalMovesPlayed = 0;

        // --- RASTGELE SESSION KURULUMU ---
        // 1. Bu maçın zorluk başlangıcını belirle (Örn: 0.92 geldi)
        _currentStartThreshold = Random.Range(startThresholdRange.x, startThresholdRange.y);

        // 2. Bu maçın ısınma süresini belirle (Örn: 35 hamle sürecek)
        _targetWarmUpMoves = Random.Range(warmUpMovesRange.x, warmUpMovesRange.y);

        Debug.Log($"<color=cyan>[SESSION]</color> Hamle Hedefi: {_targetWarmUpMoves} | Başlangıç Zorluğu: {_currentStartThreshold:F2}");

        SpawnSet();
    }

    // --- REVIVE GÜNCELLEMESİ ---
    public void ActivateReviveMode()
    {
        // Oyuncuya ekstra 20 hamlelik bir "Isınma" kredisi veriyoruz.
        // Bu, GetDynamicThreshold hesaplamasındaki progress oranını düşürür ve
        // eşiği tekrar yukarı (0.80 - 0.90 seviyelerine) çeker.
        int extraMoves = 20;
        _targetWarmUpMoves = _totalMovesPlayed + extraMoves;

        Debug.Log($"<color=green><b>[REVIVE] {extraMoves} Ekstra Hamle Eklendi! Zorluk Resetlendi.</b></color>");

        // Oyuncuyu hemen rahatlatmak için güvenli blokları spawnla
        SpawnReviveBlocks();
    }

    // --- HAMLE BAZLI DİNAMİK EŞİK ---
    private float GetDynamicThreshold()
    {
        // Eğer hedef ısınma hamlesini geçtiysek direkt en düşük (en zor/rastgele) zorluğu döndür
        if (_totalMovesPlayed >= _targetWarmUpMoves) return minThreshold;

        // İlerleme oranı (0.0 ile 1.0 arası)
        // 0.0: Oyun başı (Çok Seçici)
        // 1.0: Isınma bitti (Daha Rastgele)
        float progress = (float)_totalMovesPlayed / (float)_targetWarmUpMoves;

        // Başlangıç zorluğundan (örn 0.90), min zorluğa (0.45) doğru kay
        return Mathf.Lerp(_currentStartThreshold, minThreshold, progress);
    }

    private SpawnerMode DetermineMode(float fill)
    {
        // Doluluk %15 altındaysa her zaman Relax
        if (fill < 0.15f) return SpawnerMode.Relax;

        // Doluluk kritik seviyeyi geçtiyse Critical
        if (fill > criticalThreshold) return SpawnerMode.Critical;

        // Isınma hamlesi henüz bitmediyse WarmUp
        if (_totalMovesPlayed < _targetWarmUpMoves) return SpawnerMode.WarmUp;

        // Diğer durumlar
        if (fill > 0.5f) return SpawnerMode.SmartHelp;
        return SpawnerMode.SkillBased;
    }

    private List<BlockShapeSO> GetPoolForMode(SpawnerMode mode, Grid grid)
    {
        switch (mode)
        {
            case SpawnerMode.Relax:
                return GetRelaxPool(grid);
            case SpawnerMode.WarmUp:
                return GetWarmUpPool(grid); // <-- DİNAMİK EŞİK BURADA KULLANILIYOR
            case SpawnerMode.Critical:
                return ShapeFinder.GetFits(grid, easyShapes);
            case SpawnerMode.SmartHelp:
                return ShapeFinder.GetHoleFillers(grid, allShapes, 0.7f);
            default:
                return ShapeFinder.GetFits(grid, allShapes);
        }
    }

    private List<BlockShapeSO> GetWarmUpPool(Grid grid)
    {
        List<BlockShapeSO> allCandidates = new List<BlockShapeSO>(allShapes);

        // 1. MEGA KILL (Ödül her zaman önceliklidir, değişmez)
        var megaKillers = ShapeFinder.GetMegaKillers(grid, allCandidates);
        if (megaKillers.Count > 0)
        {
            // Debug.Log($"[WARMUP KARAR] <color=green><b>1. MEGA KILL</b></color>");
            return megaKillers;
        }

        // --- DİNAMİK HAMLE BAZLI EŞİK ---
        float dynamicThreshold = GetDynamicThreshold();

        // Debug.Log($"[HAMLE: {_totalMovesPlayed}/{_targetWarmUpMoves}] Eşik: {dynamicThreshold:F2}");

        var perfectFits = ShapeFinder.GetLargePerfectFits(grid, allCandidates, dynamicThreshold);

        if (perfectFits.Count > 0)
        {
            // Debug.Log($"[WARMUP KARAR] <color=cyan><b>2. MÜKEMMEL UYUM</b></color> (Eşik: {dynamicThreshold:F2})");
            return perfectFits;
        }

        // Hole Filler (Kilit Açma)
        // Eşiğin biraz altını kabul et (Örn: Eşik 0.8 ise bu 0.65 kabul etsin)
        var keys = ShapeFinder.GetHoleFillers(grid, allCandidates, dynamicThreshold - 0.15f);
        if (keys.Count > 0)
        {
            // Debug.Log($"[WARMUP KARAR] <color=magenta><b>4. KİLİT AÇMA</b></color>");
            return keys;
        }
        
        // Clean Kill (Temiz Patlatma)
        var cleanKillers = ShapeFinder.GetCleanKillers(grid, allCandidates);
        if (cleanKillers.Count > 0) return cleanKillers;

        // Rastgele Sığanlar
        var fits = ShapeFinder.GetFits(grid, allCandidates);
        if (fits.Count > 0) return fits;

        // Acil Durum
        return ShapeFinder.GetFits(grid, easyShapes);
    }

    // --- BENZERSİZ KARIŞIM ALGORİTMASI ---
    private List<BlockShapeSO> GenerateUniqueBatch(List<BlockShapeSO> primary, int count)
    {
        List<BlockShapeSO> batch = new List<BlockShapeSO>();
        if (primary == null || primary.Count == 0) return batch;

        List<BlockShapeSO> pool = new List<BlockShapeSO>(primary);

        if (pool.Count < count) pool.AddRange(allShapes);

        for (int i = 0; i < pool.Count; i++)
        {
            var temp = pool[i];
            int rnd = Random.Range(i, pool.Count);
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        HashSet<BlockShapeSO> selected = new HashSet<BlockShapeSO>();
        foreach (var s in pool)
        {
            if (batch.Count >= count) break;
            if (selected.Add(s)) batch.Add(s);
        }

        while (batch.Count < count) batch.Add(easyShapes[0]);

        return batch;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // --- YARDIMCILAR ---
    private List<BlockShapeSO> GetRelaxPool(Grid grid)
    {
        var p = new List<BlockShapeSO>(bigShapes);
        p.AddRange(cleanShapes);
        var f = ShapeFinder.GetFits(grid, p);
        return f.Count > 0 ? f : ShapeFinder.GetFits(grid, allShapes);
    }

    private void LogModeChange(SpawnerMode newMode, float fill)
    {
        if (_currentMode == SpawnerMode.WarmUp && newMode != SpawnerMode.WarmUp)
            Debug.Log($"<color=red>=== ISINMA SÜRESİ DOLDU (Hamle Hedefi: {_targetWarmUpMoves} ulaşıldı) ===</color>");

        if (_currentMode != newMode)
        {
            _currentMode = newMode;
        }
    }

    public void SpawnReviveBlocks()
    {
        ClearActiveBlocks();
        var saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes);

        List<BlockShapeSO> batch = new List<BlockShapeSO>();
        for (int i = 0; i < slots.Length; i++)
            if (saviors.Count > 0)
                batch.Add(saviors[Random.Range(0, saviors.Count)]);

        for (int i = 0; i < batch.Count; i++)
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(batch[i]);
            _activeBlocks.Add(b);
        }
    }

    public void SpawnSet()
    {
        // 1. HAMLE SAYACINI ARTTIR
        // Set yenileniyorsa oyuncu hamlesini yapmıştır.
        _totalMovesPlayed++;

        // Temizlik
        foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        Grid grid = GridManager.Instance.LevelGrid;
        float fill = grid.GetFillPercentage();

        // 2. Modu Belirle (Artık süre yok, hamle var)
        SpawnerMode calculatedMode = DetermineMode(fill);
        LogModeChange(calculatedMode, fill);

        // 3. HAVUZLARI HAZIRLA
        List<BlockShapeSO> primaryPool = GetPoolForMode(calculatedMode, grid);

        List<BlockShapeSO> secondaryRaw = (calculatedMode == SpawnerMode.Relax || calculatedMode == SpawnerMode.WarmUp)
            ? cleanShapes
            : allShapes;

        List<BlockShapeSO> secondaryPool = ShapeFinder.GetFits(grid, secondaryRaw);

        // 4. Batch
        List<BlockShapeSO> batch = GenerateUniqueBatch(grid, primaryPool, secondaryPool, slots.Length);

        // 5. Spawn
        for (int i = 0; i < batch.Count; i++)
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(batch[i]);
            _activeBlocks.Add(b);
        }

        CheckGameOver();
    }

    // --- BENZERSİZ VE GARANTİLİ KARIŞIM (Aynen korundu) ---
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
            while (finalBatch.Count < count && easyFits.Count > 0)
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

        Debug.Log("GAME OVER: Yerleşecek yer kalmadı!");
        GameManager.Instance.TriggerGameOver();
    }
    public void OnLinesCleared(int linesCount)
    {
        // Eğer ısınma çoktan bittiyse (oyun oturduysa), belki küçük bir miktar geri sarabiliriz
        // veya hiç karışmayız. Ama senin isteğin "Eşiğe eklensin" olduğu için:
        
        // Örn: 3 satır sildin. rewardMovesPerLine = 2 ise.
        // Toplam 6 hamlelik bir kredi kazanırsın.
        int bonusMoves = linesCount * rewardMovesPerLine;

        _targetWarmUpMoves += bonusMoves;

        // Debug.Log($"[ÖDÜL] {linesCount} satır silindi. +{bonusMoves} Hamle eklendi. Yeni Hedef: {_targetWarmUpMoves}");
    }
    private void ClearActiveBlocks()
    {
        foreach (var b in _activeBlocks)
            if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();
    }
}