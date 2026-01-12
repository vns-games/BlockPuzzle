using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public enum SpawnerMode
{
    None,
    WarmUp,         // Öncelik: Multi -> Hole -> Single -> Fit
    Critical,       // Kritik: %85+
    CriticalFail,   // Kritik: Kota Doldu
    TotalWipeout,   // God Mode
    SmartHelp,      // Akıllı Yardım
    TidyUp,         // Tamirci
    Relax,          // Rahat
    SkillBased      // Beceri
}

public class BlockSpawner : Singleton<BlockSpawner>
{
    [Header("Shapes Lists")]
    public List<BlockShapeSO> allShapes;
    public List<BlockShapeSO> easyShapes; // Küçük (1x1, 1x2, 1x3)
    public List<BlockShapeSO> bigShapes;  // Büyük (2x2, 3x3)

    [Tooltip("Sadece Kareler ve Düz Çubuklar")]
    public List<BlockShapeSO> cleanShapes;

    [Tooltip("L, J, T gibi Köşe Parçaları")]
    public List<BlockShapeSO> cornerShapes;

    [Header("References")]
    public DraggableBlock blockPrefab;
    public Transform[] slots;

    [Header("Session Randomization")]
    public Vector2 warmUpRange = new Vector2(120f, 180f);
    public Vector2 rampDurationRange = new Vector2(60f, 120f);
    public Vector2 endThresholdRange = new Vector2(0.40f, 0.55f);

    private float _currentWarmUpTime;
    private float _currentRampDuration;
    private float _currentEndThreshold;

    [Header("Survival Settings")]
    [Range(0f, 1f)] public float startHelpThreshold = 0.0f;
    [Range(0f, 1f)] public float criticalThreshold = 0.85f;

    public int maxCriticalRescueTurns = 3;
    private int _currentCriticalRescueCount = 0;

    private List<DraggableBlock> _activeBlocks = new();
    private List<BlockShapeSO> _lastTurnShapes = new List<BlockShapeSO>();
    private float _gameStartTime;
    private SpawnerMode _currentMode = SpawnerMode.None;

    public void StartGame()
    {
        _gameStartTime = Time.time;
        _lastTurnShapes.Clear();
        _currentMode = SpawnerMode.None;
        _currentCriticalRescueCount = 0;

        GenerateRandomSession();
        SpawnSet();
    }
    
    public void ExtendWarmUpAction(float extraSeconds)
    {
        _currentWarmUpTime += extraSeconds;
         Debug.Log($"+{extraSeconds}sn");
    }

    private void GenerateRandomSession()
    {
        _currentWarmUpTime = Random.Range(warmUpRange.x, warmUpRange.y);
        _currentRampDuration = Random.Range(rampDurationRange.x, rampDurationRange.y);
        _currentEndThreshold = Random.Range(endThresholdRange.x, endThresholdRange.y);

        float peak = (_currentWarmUpTime + _currentRampDuration) / 60f;
        Debug.Log($"<color=cyan>[SESSION]</color> Isınma: {_currentWarmUpTime / 60f:F1}dk | Max Zorluk: {peak:F1}dk sonra");
    }

    private float GetCurrentPanicThreshold()
    {
        float timeElapsed = Time.time - _gameStartTime;
        if (timeElapsed < _currentWarmUpTime) return 0.0f; 
        float rampProgress = Mathf.Clamp01((timeElapsed - _currentWarmUpTime) / _currentRampDuration);
        return Mathf.Lerp(startHelpThreshold, _currentEndThreshold, rampProgress);
    }

    private void LogModeChange(SpawnerMode newMode, float fillPercent)
    {
        if (_currentMode == SpawnerMode.WarmUp && newMode != SpawnerMode.WarmUp)
        {
            Debug.Log($"<color=red><b>=== ISINMA BİTTİ ===</b></color>");
        }

        if (_currentMode != newMode)
        {
            _currentMode = newMode;
            // Debug.Log($"[MOD: {newMode}] %{fillPercent * 100:F0}");
        }
    }

    // --- ANA SPAWN MANTIĞI ---
    public void SpawnSet()
    {
        ClearActiveBlocks();

        float fillPercent = GridManager.Instance.GetFillPercentage();
        float currentThreshold = GetCurrentPanicThreshold();
        float timeElapsed = Time.time - _gameStartTime;

        List<BlockShapeSO> targetPool = new List<BlockShapeSO>();
        // YEDEK HAVUZ: Eğer ana havuzdaki (örn: mükemmel parça) çeşitlilik tükenirse buradan çekeceğiz.
        List<BlockShapeSO> fallbackPool = new List<BlockShapeSO>(); 

        List<BlockShapeSO> currentBatchShapes = new List<BlockShapeSO>();
        SpawnerMode calculatedMode = SpawnerMode.None;

        // Varsayılan yedek havuz tüm şekillerdir
        fallbackPool = allShapes;

        // -----------------------------------------------------------
        // 1. MOD SEÇİMİ VE HAVUZ BELİRLEME
        // -----------------------------------------------------------

        // A) TAZE BAŞLANGIÇ (Grid %10'dan az dolu)
        if (fillPercent < 0.10f)
        {
            calculatedMode = SpawnerMode.Relax;
            fallbackPool = cleanShapes; // Taze başlangıçta yedekler de temiz olsun
            
            targetPool = new List<BlockShapeSO>();
            if (bigShapes.Count > 0) targetPool.AddRange(bigShapes);
            foreach (var shape in cleanShapes)
            {
                var mat = shape.ToMatrix().Trim();
                int cellCount = 0;
                for(int x = 0; x < mat.GetLength(0); x++) for(int y = 0; y < mat.GetLength(1); y++) if (mat[x, y]) cellCount++;     
                if (cellCount >= 4 && !targetPool.Contains(shape)) targetPool.Add(shape);
            }
            if (targetPool.Count == 0) targetPool = new List<BlockShapeSO>(cleanShapes);
        }
        // B) ISINMA MODU (WarmUp)
        else if (timeElapsed < _currentWarmUpTime)
        {
            calculatedMode = SpawnerMode.WarmUp;
            fallbackPool = cleanShapes; // WarmUp sırasında yedekler hep temiz olsun

            List<BlockShapeSO> analysisPool = new List<BlockShapeSO>();
            if (cleanShapes != null) analysisPool.AddRange(cleanShapes);
            if (cornerShapes != null) analysisPool.AddRange(cornerShapes);
            if (bigShapes != null) analysisPool.AddRange(bigShapes);
            if (analysisPool.Count == 0) analysisPool.AddRange(allShapes);

            // 1. Multi-Kill
            int maxLines;
            List<BlockShapeSO> comboKillers = GridManager.Instance.GetBestComboShapes(analysisPool, out maxLines);

            if (maxLines >= 2)
            {
                targetPool = comboKillers;
            }
            else
            {
                // 2. Kilit Açma
                List<BlockShapeSO> keys = GridManager.Instance.GetHoleFillingShapes(analysisPool, 0.70f);

                if (keys.Count > 0)
                {
                    keys.Sort((a, b) => GetShapeMass(b).CompareTo(GetShapeMass(a))); 
                    int bestMass = GetShapeMass(keys[0]);
                    targetPool = keys.FindAll(x => GetShapeMass(x) == bestMass);
                }
                else
                {
                    // 3. Tekli Satır Silme
                    if (maxLines == 1)
                    {
                        targetPool = comboKillers;
                    }
                    else
                    {
                        // 4. Hayatta Kalma
                        List<BlockShapeSO> cleanSurvivors = GridManager.Instance.GetGapFillingShapes(cleanShapes);
                        if (cleanSurvivors.Count > 0) targetPool = cleanSurvivors;
                        else
                        {
                            List<BlockShapeSO> messySurvivors = GridManager.Instance.GetGapFillingShapes(analysisPool);
                            targetPool = (messySurvivors.Count > 0) ? messySurvivors : GridManager.Instance.GetGapFillingShapes(easyShapes);
                        }
                    }
                }
            }
        }
        // C) KRİTİK VE DİĞER MODLAR
        else if (fillPercent > criticalThreshold)
        {
            if (_currentCriticalRescueCount < maxCriticalRescueTurns)
            {
                targetPool = GridManager.Instance.GetGapFillingShapes(easyShapes); 
                if (targetPool.Count == 0) targetPool = GridManager.Instance.GetGapFillingShapes(allShapes);
                calculatedMode = SpawnerMode.Critical;
                _currentCriticalRescueCount++;
            }
            else
            {
                targetPool = GridManager.Instance.GetGapFillingShapes(allShapes); 
                calculatedMode = SpawnerMode.CriticalFail;
            }
        }
        else // Normal Akış
        {
            _currentCriticalRescueCount = 0;

            if (fillPercent >= currentThreshold)
            {
                calculatedMode = SpawnerMode.SmartHelp;
                List<BlockShapeSO> wipeout = GridManager.Instance.GetTotalClearShapes(allShapes);
                if (wipeout.Count > 0)
                {
                    targetPool = wipeout;
                    calculatedMode = SpawnerMode.TotalWipeout;
                }
                else
                {
                    List<BlockShapeSO> perfectFits = GridManager.Instance.GetHoleFillingShapes(allShapes, 0.75f);
                    if (perfectFits.Count > 0) targetPool = perfectFits;
                    else
                    {
                        List<BlockShapeSO> fitting = GridManager.Instance.GetGapFillingShapes(allShapes);
                        targetPool = fitting;
                    }
                }
            }
            else // Beceri Modu
            {
                List<BlockShapeSO> fittingBigs = GridManager.Instance.GetGapFillingShapes(bigShapes);
                if (fillPercent < 0.3f && fittingBigs.Count > 0)
                {
                    targetPool = fittingBigs;
                    calculatedMode = SpawnerMode.Relax;
                }
                else
                {
                    targetPool = GridManager.Instance.GetGapFillingShapes(allShapes);
                    calculatedMode = SpawnerMode.SkillBased;
                }
            }
        }

        LogModeChange(calculatedMode, fillPercent);

        // -----------------------------------------------------------
        // 2. PARÇA SEÇİMİ (YEDEK HAVUZLU SİSTEM)
        // -----------------------------------------------------------
        for(int i = 0; i < slots.Length; i++)
        {
            // Ana havuzdan veya yedek havuzdan benzersiz parça seç
            BlockShapeSO shapeToSpawn = GetVariedShape(targetPool, fallbackPool, currentBatchShapes);

            // Eğer hala null ise (Hiçbir şey sığmıyorsa) acil durum
            if (shapeToSpawn == null)
            {
                var emergency = GridManager.Instance.GetGapFillingShapes(allShapes);
                if (emergency.Count > 0) shapeToSpawn = emergency[Random.Range(0, emergency.Count)];
            }

            if (shapeToSpawn != null) currentBatchShapes.Add(shapeToSpawn);
        }

        // -----------------------------------------------------------
        // 3. WARMUP IMMORTALITY (ÖLÜMSÜZLÜK PROTOKOLÜ)
        // -----------------------------------------------------------
        bool isAnyPiecePlayable = false;
        foreach (var shape in currentBatchShapes)
        {
            if (GridManager.Instance.CanFitAnywhere(new BlockData(shape.ToMatrix().Trim())))
            {
                isAnyPiecePlayable = true;
                break;
            }
        }

        if (!isAnyPiecePlayable && calculatedMode == SpawnerMode.WarmUp)
        {
            // Debug.LogWarning("WarmUp Ölüm Riski! Kurtarıcı Parça devreye giriyor.");
            List<BlockShapeSO> guaranteedFits = GridManager.Instance.GetGapFillingShapes(easyShapes);

            if (guaranteedFits.Count > 0)
            {
                BlockShapeSO savior = guaranteedFits[Random.Range(0, guaranteedFits.Count)];
                int replaceIndex = Random.Range(0, currentBatchShapes.Count);
                if (currentBatchShapes.Count > replaceIndex) currentBatchShapes[replaceIndex] = savior;
                else currentBatchShapes.Add(savior);
            }
        }
        else if (!isAnyPiecePlayable)
        {
            List<BlockShapeSO> saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
            if (saviors.Count > 0)
            {
                BlockShapeSO savior = saviors[Random.Range(0, saviors.Count)];
                if (currentBatchShapes.Count > 0) currentBatchShapes[Random.Range(0, currentBatchShapes.Count)] = savior;
            }
        }

        // -----------------------------------------------------------
        // 4. SPAWN (INSTANTIATE)
        // -----------------------------------------------------------
        for(int i = 0; i < currentBatchShapes.Count; i++)
        {
            if (i < slots.Length) SpawnOne(i, currentBatchShapes[i]);
        }

        _lastTurnShapes = new List<BlockShapeSO>(currentBatchShapes);
        CheckGameOver();
    }

    // --- GÜNCELLENMİŞ ÇEŞİTLİLİK FONKSİYONU ---
    // Eğer ana havuzda benzersiz parça kalmadıysa, yedek havuzu kullanır.
    private BlockShapeSO GetVariedShape(List<BlockShapeSO> primaryPool, List<BlockShapeSO> fallbackPool, List<BlockShapeSO> currentBatch)
    {
        // 1. Önce Ana Havuzdan benzersiz bulmaya çalış
        BlockShapeSO result = TryGetUniqueFrom(primaryPool, currentBatch);
        if (result != null) return result;

        // 2. Ana havuz tükendiyse (veya tek çeşit varsa ve onu kullandıysak), Yedek Havuzdan benzersiz bul
        result = TryGetUniqueFrom(fallbackPool, currentBatch);
        if (result != null) return result;

        // 3. Eğer yedek havuzda bile benzersiz yoksa, mecburen ana havuzdan rastgele ver (Aynı parça gelebilir)
        if (primaryPool != null && primaryPool.Count > 0)
            return primaryPool[Random.Range(0, primaryPool.Count)];
        
        // 4. En kötü ihtimal
        if (fallbackPool != null && fallbackPool.Count > 0)
            return fallbackPool[Random.Range(0, fallbackPool.Count)];

        return null;
    }

    private BlockShapeSO TryGetUniqueFrom(List<BlockShapeSO> pool, List<BlockShapeSO> currentBatch)
    {
        if (pool == null || pool.Count == 0) return null;

        List<BlockShapeSO> candidates = new List<BlockShapeSO>(pool);
        
        // Şu anki sette olanları çıkar
        candidates.RemoveAll(x => currentBatch.Contains(x));

        // Eğer hala aday varsa
        if (candidates.Count > 0)
        {
            // Geçmiş turdakileri de filtrelemeyi dene
            List<BlockShapeSO> historyFiltered = new List<BlockShapeSO>(candidates);
            historyFiltered.RemoveAll(x => _lastTurnShapes.Contains(x));

            if (historyFiltered.Count > 0) candidates = historyFiltered;

            return candidates[Random.Range(0, candidates.Count)];
        }
        
        return null; // Bu havuzda benzersiz parça kalmamış
    }

    // --- YARDIMCI FONKSİYONLAR ---
    public void OnBlockPlaced(DraggableBlock block)
    {
        _activeBlocks.Remove(block);
        if (_activeBlocks.Count == 0) SpawnSet();
        else CheckGameOver();
    }

    private void SpawnOne(int index, BlockShapeSO shape)
    {
        if (shape == null) return;
        DraggableBlock block = Instantiate(blockPrefab, slots[index].position, Quaternion.identity);
        block.Initialize(shape);
        _activeBlocks.Add(block);
    }

    private void CheckGameOver()
    {
        if (_activeBlocks.Count == 0)
        {
            GameManager.Instance.TriggerGameOver();
            return;
        }
        foreach (var block in _activeBlocks)
            if (GridManager.Instance.CanFitAnywhere(block.GetData()))
                return;
        GameManager.Instance.TriggerGameOver();
    }

    private void ClearActiveBlocks()
    {
        foreach (var b in _activeBlocks)
            if (b != null) Destroy(b.gameObject);
        _activeBlocks.Clear();
    }

    public void SpawnReviveBlocks()
    {
        ClearActiveBlocks();
        _currentCriticalRescueCount = 0;
        List<BlockShapeSO> saviors = GridManager.Instance.GetBestComboShapes(allShapes, out _);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetHoleFillingShapes(allShapes, 0.5f);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes);
        for(int i = 0; i < slots.Length; i++)
        {
            if (saviors.Count > 0)
            {
                BlockShapeSO shape = saviors[Random.Range(0, saviors.Count)];
                SpawnOne(i, shape);
            }
        }
    }
    
    private int GetShapeMass(BlockShapeSO shape)
    {
        if (shape == null) return 0;
        var mat = shape.ToMatrix(); 
        int count = 0;
        foreach (var cell in mat) if (cell) count++;
        return count;
    }
    
#if UNITY_EDITOR
    [ContextMenu("Şekilleri Otomatik Sınıflandır")]
    public void AutoClassifyShapes()
    {
        if (allShapes == null) return;
        easyShapes = new List<BlockShapeSO>(); 
        bigShapes = new List<BlockShapeSO>();
        cleanShapes = new List<BlockShapeSO>(); 
        cornerShapes = new List<BlockShapeSO>(); 

        foreach (var shape in allShapes)
        {
            if (shape == null) continue;
            var matrix = shape.ToMatrix().Trim(); BlockData data = new BlockData(matrix);
            int filled = 0; int area = data.Width * data.Height;
            for(int x=0;x<data.Width;x++) for(int y=0;y<data.Height;y++) if(data.Matrix[x,y]) filled++;
            
            if (data.Width == 1 && data.Height == 1) continue; 

            bool isRect = (filled == area); 

            if (isRect) 
            {
                if (!cleanShapes.Contains(shape)) cleanShapes.Add(shape);
                if (data.Width >= 2 && data.Height >= 2) { if (!bigShapes.Contains(shape)) bigShapes.Add(shape); }
                else if (filled <= 3) { if (!easyShapes.Contains(shape)) easyShapes.Add(shape); }
            }
            else
            {
                bool isThickL = (filled == area - 1); 
                bool isThinL = (filled == data.Width + data.Height - 1);
                if (data.Width >= 2 && data.Height >= 2 && (isThickL || isThinL))
                {
                    if (!cornerShapes.Contains(shape)) cornerShapes.Add(shape);
                }
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Sınıflandırma: {cleanShapes.Count} Temiz, {cornerShapes.Count} Köşe, {easyShapes.Count} Kolay, {bigShapes.Count} Büyük.");
    }
#endif
}