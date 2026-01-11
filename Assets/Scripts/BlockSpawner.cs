using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public enum SpawnerMode
{
    None,
    WarmUp,         // Öncelik: Multi-Kill -> Kilit Açma -> Tekli Kill -> Garanti Yaşam
    Critical,       
    CriticalFail,   
    TotalWipeout,   
    SmartHelp,      
    TidyUp,         
    Relax,          
    SkillBased      
}

public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner Instance { get; private set; }

    [Header("Shapes Lists")]
    public List<BlockShapeSO> allShapes;    
    public List<BlockShapeSO> easyShapes;   // Küçük (1x1, 1x2, 1x3)
    public List<BlockShapeSO> bigShapes;    // Büyük (2x2, 3x3)
    
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

    void Awake() => Instance = this;

    public void StartGame()
    {
        _gameStartTime = Time.time;
        _lastTurnShapes.Clear();
        _currentMode = SpawnerMode.None;
        _currentCriticalRescueCount = 0; 
        
        GenerateRandomSession();
        SpawnSet();
    }

    private void GenerateRandomSession()
    {
        _currentWarmUpTime = Random.Range(warmUpRange.x, warmUpRange.y);
        _currentRampDuration = Random.Range(rampDurationRange.x, rampDurationRange.y);
        _currentEndThreshold = Random.Range(endThresholdRange.x, endThresholdRange.y);
        
        float peak = (_currentWarmUpTime + _currentRampDuration) / 60f;
        Debug.Log($"<color=cyan>[SESSION]</color> Isınma: {_currentWarmUpTime/60f:F1}dk | Max Zorluk: {peak:F1}dk sonra");
    }

    // EKSİK OLAN FONKSİYON EKLENDİ
    private float GetCurrentPanicThreshold()
    {
        float timeElapsed = Time.time - _gameStartTime;
        if (timeElapsed < _currentWarmUpTime) return 0.0f; // WarmUp boyunca hep yardım et
        float rampProgress = Mathf.Clamp01((timeElapsed - _currentWarmUpTime) / _currentRampDuration);
        return Mathf.Lerp(startHelpThreshold, _currentEndThreshold, rampProgress);
    }

    private void LogModeChange(SpawnerMode newMode, float fillPercent)
    {
        if (_currentMode != newMode)
        {
            _currentMode = newMode;
            // Debug.Log($"[MOD: {newMode}] Grid: %{fillPercent*100:F0}");
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
        List<BlockShapeSO> currentBatchShapes = new List<BlockShapeSO>();
        SpawnerMode calculatedMode = SpawnerMode.None;

        // -----------------------------------------------------------
        // 1. MOD SEÇİMİ VE HAVUZ BELİRLEME
        // -----------------------------------------------------------

        // A) TAZE BAŞLANGIÇ (Grid %10'dan az dolu) -> Sadece Temel Atma
        if (fillPercent < 0.10f)
        {
            calculatedMode = SpawnerMode.Relax;
            targetPool = new List<BlockShapeSO>();
            if (bigShapes.Count > 0) targetPool.AddRange(bigShapes);
            foreach (var shape in cleanShapes)
            {
                // Sadece büyük (4 birim+) temiz parçaları al
                var mat = shape.ToMatrix().Trim();
                int cellCount = 0;
                for(int x=0;x<mat.GetLength(0);x++) for(int y=0;y<mat.GetLength(1);y++) if(mat[x,y]) cellCount++;
                if (cellCount >= 4 && !targetPool.Contains(shape)) targetPool.Add(shape);
            }
            if (targetPool.Count == 0) targetPool = new List<BlockShapeSO>(cleanShapes);
        }
        // B) ISINMA MODU (WarmUp)
        else if (timeElapsed < _currentWarmUpTime)
        {
            calculatedMode = SpawnerMode.WarmUp;
            
            // Tüm işe yarar parçaları birleştir (Analiz Havuzu)
            List<BlockShapeSO> analysisPool = new List<BlockShapeSO>();
            if (cleanShapes != null) analysisPool.AddRange(cleanShapes);
            if (cornerShapes != null) analysisPool.AddRange(cornerShapes);
            if (bigShapes != null) analysisPool.AddRange(bigShapes);
            if (analysisPool.Count == 0) analysisPool.AddRange(allShapes);

            // 1. ÖNCELİK: MULTI-KILL (Birden fazla satır silen)
            int maxLines;
            List<BlockShapeSO> comboKillers = GridManager.Instance.GetBestComboShapes(analysisPool, out maxLines);

            if (maxLines >= 2)
            {
                targetPool = comboKillers;
            }
            else
            {
                // 2. ÖNCELİK: KİLİT AÇMA (Hole Filler)
                // Eşiği biraz esnek (0.70) tutuyoruz ki "neredeyse uyan" parçaları da versin.
                List<BlockShapeSO> keys = GridManager.Instance.GetHoleFillingShapes(analysisPool, 0.70f);

                if (keys.Count > 0)
                {
                    targetPool = keys;
                }
                else
                {
                    // 3. ÖNCELİK: TEKLİ SATIR SİLME
                    if (maxLines == 1)
                    {
                        targetPool = comboKillers;
                    }
                    else
                    {
                        // 4. ÖNCELİK: HAYATTA KALMA (Sığan Herhangi Temiz Parça)
                        List<BlockShapeSO> survivors = GridManager.Instance.GetGapFillingShapes(analysisPool);
                        targetPool = (survivors.Count > 0) ? survivors : GridManager.Instance.GetGapFillingShapes(easyShapes);
                    }
                }
            }
        }
        // C) KRİTİK VE DİĞER MODLAR
        else if (fillPercent > criticalThreshold)
        {
            if (_currentCriticalRescueCount < maxCriticalRescueTurns)
            {
                targetPool = GridManager.Instance.GetGapFillingShapes(easyShapes); // Kurtarma
                if (targetPool.Count == 0) targetPool = GridManager.Instance.GetGapFillingShapes(allShapes);
                calculatedMode = SpawnerMode.Critical;
                _currentCriticalRescueCount++; 
            }
            else
            {
                targetPool = GridManager.Instance.GetGapFillingShapes(allShapes); // Ölüm
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
                if (wipeout.Count > 0) { targetPool = wipeout; calculatedMode = SpawnerMode.TotalWipeout; }
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
                 if (fillPercent < 0.3f && fittingBigs.Count > 0) { targetPool = fittingBigs; calculatedMode = SpawnerMode.Relax; }
                 else { targetPool = GridManager.Instance.GetGapFillingShapes(allShapes); calculatedMode = SpawnerMode.SkillBased; }
            }
        }

        LogModeChange(calculatedMode, fillPercent);

        // -----------------------------------------------------------
        // 2. PARÇA SEÇİMİ VE ÇEŞİTLİLİK
        // -----------------------------------------------------------
        for (int i = 0; i < slots.Length; i++)
        {
            BlockShapeSO shapeToSpawn = GetVariedShape(targetPool, currentBatchShapes);
            
            // Anti-Triple (3 aynı parça gelmesin)
            if (i == 2 && currentBatchShapes.Count == 2)
            {
                if (currentBatchShapes[0] == currentBatchShapes[1] && shapeToSpawn == currentBatchShapes[0])
                {
                    List<BlockShapeSO> alt = new List<BlockShapeSO>(targetPool); 
                    alt.RemoveAll(x => x == shapeToSpawn);
                    if (alt.Count == 0) { alt = GridManager.Instance.GetGapFillingShapes(allShapes); alt.RemoveAll(x => x == shapeToSpawn); }
                    if (alt.Count > 0) shapeToSpawn = alt[Random.Range(0, alt.Count)];
                }
            }
            
            // Eğer hala null ise (Havuz boşsa) acil durum
            if (shapeToSpawn == null)
            {
                 var emergency = GridManager.Instance.GetGapFillingShapes(allShapes);
                 if(emergency.Count > 0) shapeToSpawn = emergency[Random.Range(0, emergency.Count)];
            }

            if (shapeToSpawn != null) currentBatchShapes.Add(shapeToSpawn);
        }

        // -----------------------------------------------------------
        // 3. WARMUP IMMORTALITY (ÖLÜMSÜZLÜK PROTOKOLÜ)
        // Burası sihirli dokunuş. Rastgele patlatma YOK. 
        // Onun yerine, eğer seçilenler oynamıyorsa, oynayacak parça veriyoruz.
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

        // Eğer WarmUp modundaysak ve hiç oynanabilir hamle yoksa -> MÜDAHALE ET
        if (!isAnyPiecePlayable && calculatedMode == SpawnerMode.WarmUp)
        {
            Debug.LogWarning("WarmUp Ölüm Riski! Kurtarıcı Parça (1x1/1x2) devreye giriyor.");
            
            // EasyShapes içinden "Kesin Sığan" bir parça bul.
            // EasyShapes genelde en küçük parçalardır (1x1, 1x2).
            List<BlockShapeSO> guaranteedFits = GridManager.Instance.GetGapFillingShapes(easyShapes);
            
            // Eğer EasyShapes bile sığmıyorsa (Grid 100% doluysa), o zaman gerçekten Game Over'dır.
            // Ama %99 dolulukta bile 1x1 sığar.
            
            if (guaranteedFits.Count > 0)
            {
                // En küçük sığan parçayı al
                BlockShapeSO savior = guaranteedFits[Random.Range(0, guaranteedFits.Count)];
                
                // Batch'i temizle ve oyuncuya 3 tane sığan parça ver (veya 1 tane sığan, 2 tane rastgele)
                // Oyuncuyu rahatlatmak için 1 tanesini değiştirmek yeterli.
                int replaceIndex = Random.Range(0, currentBatchShapes.Count);
                
                // Listede en az 1 eleman olmalı değiştirmek için
                if (currentBatchShapes.Count > replaceIndex)
                {
                    currentBatchShapes[replaceIndex] = savior;
                }
                else
                {
                    currentBatchShapes.Add(savior);
                }
            }
        }
        else if (!isAnyPiecePlayable)
        {
            // Diğer modlarda da (örn: Kritik) şans ver, ama WarmUp kadar agresif değil.
            // En azından 1 tane sığan parça bulmaya çalış.
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
        for (int i = 0; i < currentBatchShapes.Count; i++)
        {
            if (i < slots.Length) SpawnOne(i, currentBatchShapes[i]);
        }

        _lastTurnShapes = new List<BlockShapeSO>(currentBatchShapes);
        CheckGameOver();
    }

    // --- YARDIMCI FONKSİYONLAR ---
    public void OnBlockPlaced(DraggableBlock block)
    {
        _activeBlocks.Remove(block);
        if (_activeBlocks.Count == 0) SpawnSet();
        else CheckGameOver();
    }

    private BlockShapeSO GetVariedShape(List<BlockShapeSO> pool, List<BlockShapeSO> currentBatch)
    {
        if (pool == null || pool.Count == 0) return null;
        List<BlockShapeSO> candidates = new List<BlockShapeSO>(pool);

        if (candidates.Count > currentBatch.Count + 1)
        {
            List<BlockShapeSO> temp = new List<BlockShapeSO>(candidates);
            temp.RemoveAll(x => currentBatch.Contains(x));
            if (temp.Count > 0) candidates = temp;
        }

        if (candidates.Count > 1)
        {
            List<BlockShapeSO> hist = new List<BlockShapeSO>(candidates);
            hist.RemoveAll(x => _lastTurnShapes.Contains(x));
            if (hist.Count > 0) candidates = hist;
        }

        if (candidates.Count == 0) return pool[Random.Range(0, pool.Count)];
        return candidates[Random.Range(0, candidates.Count)];
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
        if (_activeBlocks.Count == 0) { GameManager.Instance.TriggerGameOver(); return; } 
        foreach (var block in _activeBlocks) if (GridManager.Instance.CanFitAnywhere(block.GetData())) return; 
        GameManager.Instance.TriggerGameOver(); 
    }

    private void ClearActiveBlocks()
    {
        foreach (var b in _activeBlocks) if (b != null) Destroy(b.gameObject);
        _activeBlocks.Clear();
    }

    public void SpawnReviveBlocks()
    {
        ClearActiveBlocks();
        _currentCriticalRescueCount = 0;
        int maxLines;
        List<BlockShapeSO> saviors = GridManager.Instance.GetBestComboShapes(allShapes, out maxLines);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetHoleFillingShapes(allShapes, 0.5f);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes);
        for (int i = 0; i < slots.Length; i++) { if (saviors.Count > 0) { BlockShapeSO shape = saviors[Random.Range(0, saviors.Count)]; SpawnOne(i, shape); } }
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
            
            if (data.Width == 1 && data.Height == 1) continue; // 1x1'i atlama, belki easyShapes'e koymak istersin.

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