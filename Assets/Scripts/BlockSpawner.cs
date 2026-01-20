using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public enum SpawnerMode
{
    None, Relax, WarmUp, Critical, SmartHelp, SkillBased
}

public class BlockSpawner : Singleton<BlockSpawner>
{
    [Header("Shapes")]
    public List<BlockShapeSO> allShapes;    // ANA LİSTE (Her şey burada)
    public List<BlockShapeSO> easyShapes;   // Sadece Acil Durumlar (1x1, 2x1)

    [Header("Refs")]
    public DraggableBlock blockPrefab;
    public Transform[] slots;

    [Header("Settings")]
    public float criticalThreshold = 0.85f;

    [Header("Dynamic Difficulty")]
    public Vector2 startThresholdRange = new Vector2(0.85f, 0.95f);
    public float minThreshold = 0.45f;
    public Vector2Int warmUpMovesRange = new Vector2Int(20, 40);

    private int _totalMovesPlayed = 0;
    private int _targetWarmUpMoves;
    private float _currentStartThreshold;

    private SpawnerMode _currentMode = SpawnerMode.None;
    private List<DraggableBlock> _activeBlocks = new();

    [Header("Reward")]
    public int rewardMovesPerLine = 2;

    public void StartGame()
    {
        _currentMode = SpawnerMode.None;
        _totalMovesPlayed = 0;
        _currentStartThreshold = Random.Range(startThresholdRange.x, startThresholdRange.y);
        _targetWarmUpMoves = Random.Range(warmUpMovesRange.x, warmUpMovesRange.y);
        Debug.Log($"<color=cyan>[SESSION]</color> Target: {_targetWarmUpMoves} | Start Threshold: {_currentStartThreshold:F2}");
        SpawnSet();
    }

    public void ActivateReviveMode()
    {
        int extra = 20;
        _targetWarmUpMoves = _totalMovesPlayed + extra;
        Debug.Log($"<color=green>[REVIVE]</color> +{extra} Hamle!");
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
        if (fill > criticalThreshold) return SpawnerMode.Critical;
        if (_totalMovesPlayed < _targetWarmUpMoves) return SpawnerMode.WarmUp;
        if (fill < 0.15f) return SpawnerMode.Relax;
        if (fill > 0.5f) return SpawnerMode.SmartHelp;
        return SpawnerMode.SkillBased;
    }

    private List<BlockShapeSO> GetPoolForMode(SpawnerMode mode, Grid grid)
    {
        switch (mode)
        {
            case SpawnerMode.Relax:
                // Relax modu için artık özel liste yok, sığan herhangi bir şey olabilir
                // Veya 'PerfectFit' ile eşiği çok düşük tutarak (0.3f) rastgele ama saçma olmayan parça verebiliriz.
                // Şimdilik en garantisi: Sığan her şey.
                return ShapeFinder.GetFits(grid, allShapes);

            case SpawnerMode.WarmUp:
            case SpawnerMode.SkillBased:
                return GetWarmUpPool(grid); 

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
        // 1. MEGA KILL
        var megaKillers = ShapeFinder.GetMegaKillers(grid, allShapes);
        if (megaKillers.Count > 0)
        {
            Debug.Log("[STRATEGY] 1. MEGA KILL");
            return megaKillers;
        }

        float dynThreshold = GetDynamicThreshold();

        // 2. PERFECT FIT (Cuk Oturanlar)
        var perfectFits = ShapeFinder.GetLargePerfectFits(grid, allShapes, dynThreshold);
        if (perfectFits.Count > 0)
        {
            Debug.Log($"[STRATEGY] 2. PERFECT FIT (Threshold: {dynThreshold:F2})");
            return perfectFits;
        }

        // 3. HOLE FILLER (Boşluk Doldurma)
        var keys = ShapeFinder.GetHoleFillers(grid, allShapes, dynThreshold - 0.15f);
        if (keys.Count > 0)
        {
            Debug.Log("[STRATEGY] 3. HOLE FILLER");
            return keys;
        }
        
        // 4. CLEAN KILL (Temizleyici)
        var clean = ShapeFinder.GetCleanKillers(grid, allShapes);
        if (clean.Count > 0) return clean;

        // 5. STANDARD FIT (Ne varsa)
        // 'GetSatisfyingFits' ShapeFinder'da olmadığı için düz GetFits kullanıyoruz.
        var fits = ShapeFinder.GetFits(grid, allShapes);
        if (fits.Count > 0) return fits;

        // ACİL DURUM
        return ShapeFinder.GetFits(grid, easyShapes);
    }

    public void SpawnSet()
    {
        _totalMovesPlayed++;
        foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        Grid grid = GridManager.Instance.LevelGrid;
        
        BlockShapeSO rescue = FindGuaranteedFullClearBlock();
        List<BlockShapeSO> primary = null;
        SpawnerMode mode = _currentMode;

        if (rescue != null) {
            Debug.Log("<color=green>[PRIORITY 1]</color> INCREDIBLE SAVIOR!");
            primary = GetWarmUpPool(grid);
            _currentMode = SpawnerMode.SkillBased;
        }
        else {
            float fill = grid.GetFillPercentage();
            mode = DetermineMode(fill);
            if(mode != _currentMode) Debug.Log($"[SPAWNER] Mode: {mode}");
            _currentMode = mode;
            
            if (mode == SpawnerMode.Critical) {
                // FindPotentialMegaKiller metodun olmadığı için GetMegaKillers listesinden ilkini alıyoruz.
                var mkList = ShapeFinder.GetMegaKillers(grid, allShapes);
                if(mkList.Count > 0) 
                { 
                    rescue = mkList[0]; 
                    Debug.Log("[OVERRIDE] CRITICAL MEGA KILL!"); 
                }
            }
            if(primary == null) primary = GetPoolForMode(mode, grid);
        }

        var secondary = ShapeFinder.GetFits(grid, allShapes);
        var batch = GenerateUniqueBatch(grid, primary, secondary, slots.Length);
        
        if(rescue != null) batch[0] = rescue;

        for(int i=0; i<batch.Count; i++) {
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

        void TryFill(List<BlockShapeSO> src) {
            if(src==null || src.Count==0) return;
            if(finalBatch.Count >= count) return;
            var shuf = new List<BlockShapeSO>(src);
            ShuffleList(shuf);
            foreach(var s in shuf) {
                if(finalBatch.Count>=count) break;
                if(selectedSet.Contains(s)) continue;
                if(GridManager.Instance.CanFitAnywhere(new BlockData(s.ToMatrix().Trim()))) {
                    selectedSet.Add(s); finalBatch.Add(s);
                }
            }
        }
        TryFill(primary);
        TryFill(secondary);
        
        if(finalBatch.Count < count) TryFill(ShapeFinder.GetFits(grid, allShapes));
        while(finalBatch.Count < count) finalBatch.Add(easyShapes[Random.Range(0, easyShapes.Count)]);
        return finalBatch;
    }

    // --- YARDIMCILAR ---
    public void OnLinesCleared(int linesCount) 
    { 
        _targetWarmUpMoves += linesCount * rewardMovesPerLine; 
    }
    
    // (Aynen korundu)
    public BlockShapeSO FindGuaranteedFullClearBlock() {
        var grid = GridManager.Instance.LevelGrid; int w = grid.Width; int h = grid.Height; BlockShapeSO best = null; int maxC = -1; 
        foreach (var s in allShapes) { var rm = s.ToMatrix().Trim(); BlockData d = new BlockData(rm); int cc = 0; for(int i=0;i<d.Width;i++) for(int j=0;j<d.Height;j++) if(d.Matrix[i,j]) cc++; if(cc<=1) continue; bool check=(best==null)||(cc>maxC); if(!check) continue; bool ok=false;
        for(int x=0;x<w;x++) { for(int y=0;y<h;y++) { if(CanPlace(grid,d,x,y)) { if(CheckFullClear(grid,d,x,y)) { ok=true; goto End; } } } } End:; if(ok) { if(cc>maxC) { maxC=cc; best=s; } } } return best;
    }
    private bool CanPlace(Grid g, BlockData d, int x, int y) { if(x+d.Width>g.Width||y+d.Height>g.Height)return false; for(int i=0;i<d.Width;i++) for(int j=0;j<d.Height;j++) if(d.Matrix[i,j] && g.Cells[x+i,y+j]) return false; return true; }
    private bool CheckFullClear(Grid g, BlockData d, int gx, int gy) { bool[,] sim=(bool[,])g.Cells.Clone(); for(int i=0;i<d.Width;i++) for(int j=0;j<d.Height;j++) if(d.Matrix[i,j]) sim[gx+i,gy+j]=true; 
    List<int> r=new List<int>(), c=new List<int>(); for(int y=0;y<g.Height;y++) { bool f=true; for(int x=0;x<g.Width;x++) if(!sim[x,y]){f=false;break;} if(f) r.Add(y); }
    for(int x=0;x<g.Width;x++) { bool f=true; for(int y=0;y<g.Height;y++) if(!sim[x,y]){f=false;break;} if(f) c.Add(x); } foreach(int y in r) for(int x=0;x<g.Width;x++) sim[x,y]=false; foreach(int x in c) for(int y=0;y<g.Height;y++) sim[x,y]=false;
    for(int x=0;x<g.Width;x++) for(int y=0;y<g.Height;y++) if(sim[x,y]) return false; return true; }

    public void SpawnReviveBlocks() { foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject); _activeBlocks.Clear(); var saviors = GridManager.Instance.GetGapFillingShapes(easyShapes); if (saviors.Count == 0) saviors = GridManager.Instance.GetGapFillingShapes(allShapes); List<BlockShapeSO> batch = new List<BlockShapeSO>(); for(int i = 0; i < slots.Length; i++) if (saviors.Count > 0) batch.Add(saviors[Random.Range(0, saviors.Count)]); for(int i = 0; i < batch.Count; i++) { var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity); b.Initialize(batch[i]); _activeBlocks.Add(b); } }
    private void ShuffleList<T>(List<T> list) { for(int i = 0; i < list.Count; i++) { var temp = list[i]; int r = Random.Range(i, list.Count); list[i] = list[r]; list[r] = temp; } }
    public void OnBlockPlaced(DraggableBlock b) { _activeBlocks.Remove(b); if (_activeBlocks.Count == 0) SpawnSet(); else StartCoroutine(CheckOver()); }
    private IEnumerator CheckOver() { yield return new WaitForEndOfFrame(); CheckGameOver(); }
    private void CheckGameOver() { if (_activeBlocks.Count == 0) return; foreach (var b in _activeBlocks) if (GridManager.Instance.CanFitAnywhere(b.GetData())) return; GameManager.Instance.TriggerGameOver(); }
    private void ClearActiveBlocks() { foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject); _activeBlocks.Clear(); }
}