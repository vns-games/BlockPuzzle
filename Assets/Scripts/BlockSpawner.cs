using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public enum SpawnerMode
{
    None,
    Relax,
    WarmUp,         // Öncelik: MegaKill -> BigFit -> CleanKill -> AnyFit
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
    public Vector2 warmUpRange = new Vector2(120f, 180f);

    // State
    private float _warmUpDuration;
    private float _gameStartTime;
    private SpawnerMode _currentMode = SpawnerMode.None;
    private List<DraggableBlock> _activeBlocks = new();

    public void StartGame()
    {
        _gameStartTime = Time.time;
        _currentMode = SpawnerMode.None;
        
        _warmUpDuration = Random.Range(warmUpRange.x, warmUpRange.y);
        Debug.Log($"<color=cyan>[SESSION]</color> İlk Isınma Süresi: {_warmUpDuration / 60f:F1}dk");

        SpawnSet();
    }
    
    // --- YENİ EKLENEN: REVIVE WARMUP ---
    public void ActivateReviveMode()
    {
        // Şu anki geçen süreye 60 saniye ekleyerek Isınma limitini ileri atıyoruz.
        float timeElapsed = Time.time - _gameStartTime;
        _warmUpDuration = timeElapsed + 60f; 
        
        Debug.Log($"<color=green><b>[REVIVE] 60 Saniye WarmUp Modu Aktif Edildi!</b></color>");

        // Oyuncuyu hemen rahatlatmak için güvenli blokları spawnla
        SpawnReviveBlocks();
    }
    // ------------------------------------

    public void ExtendWarmUp(float seconds) => _warmUpDuration += seconds;

    

    private SpawnerMode DetermineMode(float fill, float time)
    {
        if (fill < 0.15f) return SpawnerMode.Relax;
        if (time < _warmUpDuration) return SpawnerMode.WarmUp;
        if (fill > criticalThreshold) return SpawnerMode.Critical;
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
                return GetWarmUpPool(grid); // <-- LOGLAR BURADA
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
        // ARTIK AYRIM YOK! TÜM ENVANTERİ TARA.
        // Manuel listeler (cleanShapes vs.) yerine direkt allShapes kullanıyoruz.
        // Böylece "Unutulmuş parça" veya "Listeye eklenmemiş parça" derdi bitiyor.
        List<BlockShapeSO> allCandidates = new List<BlockShapeSO>(allShapes);

        // --- 1. ÖNCELİK: MEGA KILL (3+ Satır/Sütun) ---
        // "Şov yapacak parça var mı?"
        var megaKillers = ShapeFinder.GetMegaKillers(grid, allCandidates);
        if (megaKillers.Count > 0)
        {
            Debug.Log($"[WARMUP KARAR] <color=green><b>1. MEGA KILL</b></color> - Envanterden Şov Parçası Bulundu.");
            return megaKillers;
        }

        // --- 2. ÖNCELİK: BÜYÜK VE MÜKEMMEL UYUM (TETRIS HİSSİ) ---
        // "Büyük bir boşluğa 'cuk' diye oturan var mı?"
        // Not: Burada 1x1'leri elemek için ShapeFinder içinde kütle kontrolü var (Mass >= 3 veya 4)
        var perfectFits = ShapeFinder.GetLargePerfectFits(grid, allCandidates, 0.45f); // Eşik %60 temas
        if (perfectFits.Count > 0)
        {
            Debug.Log($"[WARMUP KARAR] <color=cyan><b>2. MÜKEMMEL UYUM</b></color> - Tetris gibi oturan parça.");
            return perfectFits;
        }

        // --- 3. ÖNCELİK: TEMİZ PATLATMA ---
        // "Ortalığı kirletmeden 1-2 satır alacak var mı?"
        var cleanKillers = ShapeFinder.GetCleanKillers(grid, allCandidates);
        if (cleanKillers.Count > 0)
        {
            Debug.Log($"[WARMUP KARAR] <color=yellow><b>3. TEMİZ PATLATMA</b></color> - Temiz işçilik.");
            return cleanKillers;
        }

        // --- 4. ÖNCELİK: KİLİT AÇMA (HOLE FILLER) ---
        // "Küçük delikleri, tekli boşlukları kapatacak var mı?"
        // Eşik değerini biraz düşürdük (0.6f) ki kenardaki boşlukları da yakalasın.
        var keys = ShapeFinder.GetHoleFillers(grid, allCandidates, 0.60f);
        if (keys.Count > 0)
        {
            Debug.Log($"[WARMUP KARAR] <color=magenta><b>4. KİLİT AÇMA</b></color> - Boşluk doldurma.");
            return keys;
        }
        
        // --- 5. ÖNCELİK: HAYATTA KALMA ---
        // "Hiçbiri olmuyor, bari sığan bir şey ver de oyun bitmesin."
        var fits = ShapeFinder.GetFits(grid, allCandidates);
        if (fits.Count > 0)
        {
            Debug.Log($"[WARMUP KARAR] <color=white><b>5. RASTGELE SIĞAN</b></color> - Mecburiyet.");
            return fits;
        }

        Debug.Log($"[WARMUP KARAR] <color=red><b>ACİL DURUM</b></color> - Sığan parça yok, küçükleri dene.");
        return ShapeFinder.GetFits(grid, easyShapes);
    }

    // --- BENZERSİZ KARIŞIM ALGORİTMASI ---
    private List<BlockShapeSO> GenerateUniqueBatch(List<BlockShapeSO> primary, int count)
    {
        List<BlockShapeSO> batch = new List<BlockShapeSO>();
        if (primary == null || primary.Count == 0) return batch;

        List<BlockShapeSO> pool = new List<BlockShapeSO>(primary);
        
        // Havuzda yeterince eleman yoksa mecburen yedeğe (tüm şekillere) başvur
        if (pool.Count < count) pool.AddRange(allShapes);

        // Karıştır
        for (int i = 0; i < pool.Count; i++)
        {
            var temp = pool[i];
            int rnd = Random.Range(i, pool.Count);
            pool[i] = pool[rnd];
            pool[rnd] = temp;
        }

        // Seç (Aynı şekli 2 kere seçme)
        HashSet<BlockShapeSO> selected = new HashSet<BlockShapeSO>();
        foreach (var s in pool)
        {
            if (batch.Count >= count) break;
            if (selected.Add(s)) batch.Add(s);
        }
        
        // Hala eksikse (çok nadir), rastgele doldur
        while(batch.Count < count) batch.Add(easyShapes[0]);

        return batch;
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
            Debug.Log($"<color=red>=== ISINMA SÜRESİ DOLDU ===</color>");
        
        if (_currentMode != newMode)
        {
            _currentMode = newMode;
            // Debug.Log($"[MOD DEĞİŞTİ]: {newMode}");
        }
    }

    public void SpawnReviveBlocks()
    {
        ClearActiveBlocks();
        var saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes);

        List<BlockShapeSO> batch = new List<BlockShapeSO>();
        for (int i = 0; i < slots.Length; i++)
            if (saviors.Count > 0) batch.Add(saviors[Random.Range(0, saviors.Count)]);
        
        for(int i=0; i<batch.Count; i++)
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(batch[i]);
            _activeBlocks.Add(b);
        }
    }
    
    public void SpawnSet()
    {
        // 1. Temizlik
        foreach(var b in _activeBlocks) if(b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        Grid grid = GridManager.Instance.LevelGrid;
        float fill = grid.GetFillPercentage();
        float timeElapsed = Time.time - _gameStartTime;

        // 2. Modu Belirle
        SpawnerMode calculatedMode = DetermineMode(fill, timeElapsed);
        LogModeChange(calculatedMode, fill);

        // 3. HAVUZLARI HAZIRLA (Burada değişiklik var)
        
        // A) Birincil Havuz (Stratejik Havuz - Zaten filtrelenmiş gelir)
        List<BlockShapeSO> primaryPool = GetPoolForMode(calculatedMode, grid);

        // B) İkincil Havuz (Yedek - Doldurma)
        // HATA BURADAYDI: Eskiden direkt 'cleanShapes' veriyorduk, sığıp sığmadığına bakmıyorduk.
        // ŞİMDİ: Yedek havuzu da 'GetFits' ile süzüyoruz.
        List<BlockShapeSO> secondaryRaw = (calculatedMode == SpawnerMode.Relax || calculatedMode == SpawnerMode.WarmUp) 
                                           ? cleanShapes : allShapes;
        
        // WarmUp modundaysak YEDEKLERİN DE kesin sığması şart. Diğer modlarda da güvenli olsun.
        List<BlockShapeSO> secondaryPool = ShapeFinder.GetFits(grid, secondaryRaw);

        // 4. BENZERSİZ KARIŞIM OLUŞTUR
        // Eğer ikincil havuz bile boşsa (çok sıkışıksa), ShapeFinder.GetFits(allShapes) devreye girecek (Batch içinde).
        List<BlockShapeSO> batch = GenerateUniqueBatch(grid, primaryPool, secondaryPool, slots.Length);

        // 5. Spawn
        for(int i=0; i<batch.Count; i++)
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(batch[i]);
            _activeBlocks.Add(b);
        }
        
        CheckGameOver();
    }

    // --- BENZERSİZ VE GARANTİLİ KARIŞIM ---
    private List<BlockShapeSO> GenerateUniqueBatch(Grid grid, List<BlockShapeSO> primary, List<BlockShapeSO> secondary, int count)
    {
        HashSet<BlockShapeSO> selectedSet = new HashSet<BlockShapeSO>();
        List<BlockShapeSO> finalBatch = new List<BlockShapeSO>();

        // Yerel Yardımcı Fonksiyon: Listeden rastgele al, sığıyorsa ekle
        void TryFillFrom(List<BlockShapeSO> sourceList)
        {
            if (sourceList == null || sourceList.Count == 0) return;
            if (finalBatch.Count >= count) return;

            // Listeyi karıştır
            List<BlockShapeSO> shuffled = new List<BlockShapeSO>(sourceList);
            ShuffleList(shuffled);

            foreach (var shape in shuffled)
            {
                if (finalBatch.Count >= count) break;
                
                // Zaten seçildiyse geç
                if (selectedSet.Contains(shape)) continue;

                // --- EKSTRA GÜVENLİK ---
                // Primary zaten süzülmüş geliyor ama Secondary veya AllShapes'ten gelenleri
                // son bir kez daha grid kontrolünden geçirmek %100 garanti sağlar.
                // (Performans için; zaten süzülmüş listelerde bu adımı atlayabilirsin ama WarmUp için değer)
                if (GridManager.Instance.CanFitAnywhere(new BlockData(shape.ToMatrix().Trim())))
                {
                    selectedSet.Add(shape);
                    finalBatch.Add(shape);
                }
            }
        }

        // 1. Aşama: Strateji Havuzu (Örn: Mega Kill)
        TryFillFrom(primary);

        // 2. Aşama: Yedek Havuz (Örn: Temiz Parçalar)
        TryFillFrom(secondary);

        // 3. Aşama: Hala eksikse "Sığan Herhangi Bir Şey" (All Shapes Fits)
        if (finalBatch.Count < count)
        {
            var allFits = ShapeFinder.GetFits(grid, allShapes);
            TryFillFrom(allFits);
        }

        // 4. Aşama: ÇOK ACİL DURUM (Hala eksikse 1x1 Kurtarıcılar)
        if (finalBatch.Count < count)
        {
            var easyFits = ShapeFinder.GetFits(grid, easyShapes);
            // Burada artık set kontrolü yapmadan doldur, çünkü slot boş kalmamalı
            while (finalBatch.Count < count && easyFits.Count > 0)
            {
                var s = easyFits[Random.Range(0, easyFits.Count)];
                finalBatch.Add(s);
            }
        }

        return finalBatch;
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
    
    // Standart fonksiyonlar...
    public void OnBlockPlaced(DraggableBlock b) { _activeBlocks.Remove(b); if(_activeBlocks.Count==0) SpawnSet(); else CheckGameOver(); }
    private void CheckGameOver() { if(_activeBlocks.Count==0){GameManager.Instance.TriggerGameOver(); return;} foreach(var b in _activeBlocks) if(GridManager.Instance.CanFitAnywhere(b.GetData())) return; GameManager.Instance.TriggerGameOver(); }
    private void ClearActiveBlocks() { foreach(var b in _activeBlocks) if(b) Destroy(b.gameObject); _activeBlocks.Clear(); }
}