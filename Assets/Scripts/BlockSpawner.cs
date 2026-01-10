using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner Instance { get; private set; }

    [Header("Shapes Lists")]
    public List<BlockShapeSO> allShapes;    // TÜM PARÇALAR
    public List<BlockShapeSO> easyShapes;   // KÜÇÜK PARÇALAR (Otomatik)
    public List<BlockShapeSO> bigShapes;    // BÜYÜK PARÇALAR (Otomatik)
    
    // NOT: ReviveBlock ve SingleBlock değişkenleri SİLİNDİ.

    [Header("References")]
    public DraggableBlock blockPrefab;
    public Transform[] slots;

    [Header("Difficulty Timing")]
    public float warmUpTime = 120f; 
    public float difficultyRampDuration = 180f; 

    [Header("Threshold Settings")]
    [Range(0f, 1f)] public float startHelpThreshold = 0.0f;
    [Range(0f, 1f)] public float endHelpThreshold = 0.55f;   
    [Range(0f, 1f)] public float criticalThreshold = 0.85f;

    private List<DraggableBlock> _activeBlocks = new();
    private List<BlockShapeSO> _lastTurnShapes = new List<BlockShapeSO>();
    private float _gameStartTime;

    void Awake() => Instance = this;

    public void StartGame(List<BlockShapeSO> specificStartBlocks = null)
    {
        _gameStartTime = Time.time;
        _lastTurnShapes.Clear();
        SpawnSet(specificStartBlocks);
    }

    // --- YENİ REVIVE MANTIĞI: GARANTİ PATLATMA ---
    public void SpawnReviveBlocks()
    {
        ClearActiveBlocks();
        
        // 1. Grid'i analiz et ve KESİN satır/sütun silecek parçaları bul
        int maxLines;
        List<BlockShapeSO> saviors = GridManager.Instance.GetBestComboShapes(allShapes, out maxLines);

        // 2. Eğer patlatan parça yoksa (Çok nadir ama grid garipse olabilir)
        // O zaman en azından delikleri tıkayan (Cuk oturan) parçaları al
        if (saviors.Count == 0)
        {
            saviors = GridManager.Instance.GetHoleFillingShapes(allShapes, 0.5f);
        }

        // 3. Hala parça yoksa (Grid tıkalıysa), en küçük sığanları al
        if (saviors.Count == 0)
        {
            saviors = GridManager.Instance.GetGapFillingShapes(easyShapes);
        }

        // Güvenlik: Eğer hala boşsa, yapacak bir şey yok, rastgele sığan dene
        if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes);


        // --- SLOTLARI DOLDUR ---
        // Bulduğumuz "Kurtarıcı" (Savior) parçalardan rastgele seçip slotlara koy
        for (int i = 0; i < slots.Length; i++)
        {
            if (saviors.Count > 0)
            {
                BlockShapeSO shape = saviors[Random.Range(0, saviors.Count)];
                SpawnOne(i, shape);
            }
        }
        
        Debug.Log($"<color=green>REVIVE BAŞARILI! Patlatıcı/Kurtarıcı parçalar verildi.</color>");
    }

    // --- EŞİK HESABI ---
    private float GetCurrentPanicThreshold()
    {
        float timeElapsed = Time.time - _gameStartTime;
        if (timeElapsed < warmUpTime) return 0.0f; // İlk 2 dk hep yardım
        float rampProgress = Mathf.Clamp01((timeElapsed - warmUpTime) / difficultyRampDuration);
        return Mathf.Lerp(startHelpThreshold, endHelpThreshold, rampProgress);
    }

    public void SpawnSet(List<BlockShapeSO> overrideBlocks = null)
    {
        ClearActiveBlocks();

        float fillPercent = GridManager.Instance.GetFillPercentage();
        float currentThreshold = GetCurrentPanicThreshold(); 
        
        List<BlockShapeSO> targetPool = new List<BlockShapeSO>();
        List<BlockShapeSO> currentBatchShapes = new List<BlockShapeSO>();

        // -----------------------------------------------------------
        // 1. HAVUZ BELİRLEME
        // -----------------------------------------------------------
        
        // A) KRİTİK (%85+) -> 1x1 YOK -> KÜÇÜK SIĞANLAR VAR
        if (fillPercent > criticalThreshold)
        {
            // Sadece Easy (Küçük) listesinden sığanları getir.
            targetPool = GridManager.Instance.GetGapFillingShapes(easyShapes);
            
            // Eğer sığan küçük parça kalmadıysa genel havuza bak (Son çare)
            if (targetPool.Count == 0) targetPool = GridManager.Instance.GetGapFillingShapes(allShapes);
            
            Debug.Log("Mod: KRİTİK (Küçük Parçalar)");
        }
        // B) YARDIM MODU (Grid doluluğu > Eşik)
        else if (fillPercent >= currentThreshold)
        {
            List<BlockShapeSO> perfectFits = GridManager.Instance.GetHoleFillingShapes(allShapes, 0.75f);
            int maxLines;
            List<BlockShapeSO> bestCombos = GridManager.Instance.GetBestComboShapes(allShapes, out maxLines);

            HashSet<BlockShapeSO> mixedPool = new HashSet<BlockShapeSO>();
            if (perfectFits.Count > 0) mixedPool.UnionWith(perfectFits);
            if (bestCombos.Count > 0) mixedPool.UnionWith(bestCombos);

            if (mixedPool.Count > 0)
            {
                targetPool = new List<BlockShapeSO>(mixedPool);
            }
            else
            {
                List<BlockShapeSO> tidyShapes = GridManager.Instance.GetMostCompactShapes(allShapes);
                if (tidyShapes.Count > 0) targetPool = tidyShapes;
                else targetPool = GridManager.Instance.GetGapFillingShapes(allShapes);
            }
        }
        // C) RAHAT MOD (Grid Boş)
        else 
        {
             // Büyükler sığıyorsa ver, sığmıyorsa normale dön
             List<BlockShapeSO> fittingBigs = GridManager.Instance.GetGapFillingShapes(bigShapes);
             if (fillPercent < 0.2f && fittingBigs.Count > 0) targetPool = fittingBigs;
             else targetPool = GridManager.Instance.GetGapFillingShapes(allShapes);
        }

        // -----------------------------------------------------------
        // 2. SEÇİM VE SPAWN
        // -----------------------------------------------------------
        for (int i = 0; i < slots.Length; i++)
        {
            BlockShapeSO shapeToSpawn = null;

            if (overrideBlocks != null && i < overrideBlocks.Count)
            {
                shapeToSpawn = overrideBlocks[i];
            }
            else
            {
                // ÇEŞİTLİLİK
                shapeToSpawn = GetVariedShape(targetPool, currentBatchShapes);

                // ANTI-TRIPLE KORUMA
                if (i == 2 && currentBatchShapes.Count == 2)
                {
                    if (currentBatchShapes[0] == currentBatchShapes[1] && shapeToSpawn == currentBatchShapes[0])
                    {
                        List<BlockShapeSO> alternatives = new List<BlockShapeSO>(targetPool);
                        alternatives.RemoveAll(x => x == shapeToSpawn);

                        // Hedef havuzda alternatif yoksa genel havuza bak
                        if (alternatives.Count == 0)
                        {
                             List<BlockShapeSO> fallback = GridManager.Instance.GetGapFillingShapes(allShapes);
                             fallback.RemoveAll(x => x == shapeToSpawn);
                             alternatives = fallback;
                        }

                        if (alternatives.Count > 0) shapeToSpawn = alternatives[Random.Range(0, alternatives.Count)];
                    }
                }
            }

            if (shapeToSpawn != null)
            {
                currentBatchShapes.Add(shapeToSpawn);
                SpawnOne(i, shapeToSpawn);
            }
        }

        _lastTurnShapes = new List<BlockShapeSO>(currentBatchShapes);
        CheckGameOver();
    }

    private BlockShapeSO GetVariedShape(List<BlockShapeSO> pool, List<BlockShapeSO> currentBatch)
    {
        if (pool == null || pool.Count == 0) return null;
        List<BlockShapeSO> candidates = new List<BlockShapeSO>(pool);

        if (candidates.Count > currentBatch.Count + 1) candidates.RemoveAll(x => currentBatch.Contains(x));
        if (candidates.Count > 1) 
        {
            List<BlockShapeSO> historyFiltered = new List<BlockShapeSO>(candidates);
            historyFiltered.RemoveAll(x => _lastTurnShapes.Contains(x));
            if (historyFiltered.Count > 0) candidates = historyFiltered;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    // --- STANDART ---
    private void SpawnOne(int index, BlockShapeSO shape)
    {
        if (shape == null) return;
        DraggableBlock block = Instantiate(blockPrefab, slots[index].position, Quaternion.identity);
        block.Initialize(shape);
        _activeBlocks.Add(block);
    }
    public void OnBlockPlaced(DraggableBlock block) { _activeBlocks.Remove(block); if (_activeBlocks.Count == 0) SpawnSet(null); else CheckGameOver(); }
    private void CheckGameOver() { 
        if (_activeBlocks.Count == 0) { GameManager.Instance.TriggerGameOver(); return; } 
        foreach (var block in _activeBlocks) if (GridManager.Instance.CanFitAnywhere(block.GetData())) return; 
        GameManager.Instance.TriggerGameOver(); 
    }
    private void ClearActiveBlocks() { foreach (var b in _activeBlocks) if (b != null) Destroy(b.gameObject); _activeBlocks.Clear(); }

#if UNITY_EDITOR
    [ContextMenu("Şekilleri Otomatik Sınıflandır")]
    public void AutoClassifyShapes()
    {
        if (allShapes == null || allShapes.Count == 0) return;
        easyShapes = new List<BlockShapeSO>(); bigShapes = new List<BlockShapeSO>();
        foreach (var shape in allShapes)
        {
            if (shape == null) continue;
            var matrix = shape.ToMatrix().Trim(); BlockData data = new BlockData(matrix);
            int filled = 0; int area = data.Width * data.Height;
            for(int x=0;x<data.Width;x++) for(int y=0;y<data.Height;y++) if(data.Matrix[x,y]) filled++;
            
            // 1x1 Uyarısı yok artık, sadece kolay listesine atma
            if (data.Width == 1 && data.Height == 1) continue; 

            if (data.Width >= 2 && data.Height >= 2 && filled == area) { if (!bigShapes.Contains(shape)) bigShapes.Add(shape); }
            else if ((data.Width <= 2 && data.Height <= 2) || filled <= 3) { if (!easyShapes.Contains(shape)) easyShapes.Add(shape); }
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("Sınıflandırma Tamam (1x1'ler hariç).");
    }
#endif
}