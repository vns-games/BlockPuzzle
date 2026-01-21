using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; 
using VnS.Utility.Singleton;

public class BlockSpawner : Singleton<BlockSpawner>
{
    [Header("Master Data")]
    public List<BlockShapeSO> allShapes;    
    
    [Header("Refs")]
    public DraggableBlock blockPrefab;
    public Transform[] slots;

    private List<DraggableBlock> _activeBlocks = new List<DraggableBlock>();

    // --- PUAN BAREMLERİ (YENİ HİYERARŞİ) ---
    private const int TIER_INCREDIBLE   = 1000000; // Full Clear (En Tepe)
    
    // ARTIK EN DEĞERLİ İKİNCİ ŞEY: "CLEAN KILL" (Hem siliyor hem taşmıyor)
    private const int TIER_CLEAN_KILL   = 500000;  
    
    // PERFECT FIT (Silmiyor ama çok iyi oturuyor)
    private const int TIER_PERFECT_FIT  = 100000;  
    
    // MESSY KILL (Siliyor ama sağa sola taşıyor - Flush Değil)
    private const int TIER_MESSY_KILL   = 50000;   
    
    private const int TIER_FIT          = 1000;    // Standart

    [Header("Settings")]
    public int rewardMovesPerLine = 2; 
    private int _targetWarmUpMoves = 10; 
    private bool _isWarmUpActive = true;

    // --- GHOST ---
    private BlockShapeSO _ghostShape;
    private Vector2Int _ghostTarget;
    private bool _showGhost = false;

    public void StartGame()
    {
        Debug.Log($"<color=yellow>[START]</color> Oyun Başlatıldı. Hedef: {_targetWarmUpMoves} Hamle");
        SpawnSet();
    }

    public void ActivateReviveMode() => SpawnSet();

    public void SpawnSet()
    {
        foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        if (GridManager.Instance == null) return;
        Grid grid = GridManager.Instance.LevelGrid;
        
        List<ScoredShape> candidates = GenerateAllCandidates(grid);

        if (candidates.Count == 0)
        {
             Debug.Log("<color=red>[GAME OVER]</color> Hiçbir parça sığmıyor!");
             GameManager.Instance.TriggerGameOver();
             return;
        }

        List<ScoredShape> finalPicks = new List<ScoredShape>();
        HashSet<BlockShapeSO> usedShapes = new HashSet<BlockShapeSO>();

        for(int i=0; i<slots.Length; i++) 
        {
            ScoredShape pick = null;
            foreach(var cand in candidates)
            {
                if(!usedShapes.Contains(cand.Shape))
                {
                    pick = cand;
                    break;
                }
            }
            if (pick == null && candidates.Count > 0) pick = candidates[0];

            if (pick != null)
            {
                finalPicks.Add(pick);
                usedShapes.Add(pick.Shape);
            }
        }

        for(int i=0; i<finalPicks.Count; i++) 
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(finalPicks[i].Shape, finalPicks[i].TargetPos);
            _activeBlocks.Add(b);
        }
    }

    public void OnLinesCleared(int linesCount) 
    { 
        if (!_isWarmUpActive) return;
        _targetWarmUpMoves -= linesCount;
        if (_targetWarmUpMoves <= 0) {
            _isWarmUpActive = false;
            Debug.Log("<color=cyan>[WARM UP] BİTTİ!</color>");
        } else {
            Debug.Log($"<color=orange>[WARM UP] Kalan: {_targetWarmUpMoves}</color>");
        }
    }
    
    public void OnBlockPlaced(DraggableBlock b) 
    { 
        _activeBlocks.Remove(b); 
        if (_activeBlocks.Count == 0) SpawnSet(); 
        else StartCoroutine(CheckOver()); 
    }
    
    private IEnumerator CheckOver() { yield return new WaitForEndOfFrame(); CheckGameOver(); }
    
    private void CheckGameOver() 
    { 
        if (_activeBlocks.Count == 0) return; 
        foreach (var b in _activeBlocks) if (GridManager.Instance.CanFitAnywhere(b.GetData())) return; 
        GameManager.Instance.TriggerGameOver(); 
    }

    // ======================================================================================
    //  ANALİZ (CLEAN KILL > PERFECT FIT)
    // ======================================================================================
    private class ScoredShape { 
        public BlockShapeSO Shape; 
        public int Score; 
        public string Label; 
        public int Mass; 
        public Vector2Int TargetPos; 
    }
    
    private struct ShapeAnalysis { 
        public bool Fits; 
        public Vector2Int BestPos; 
        public int LinesCleared; 
        public float FitScore; 
        public int BlockMass; 
        public bool ResultsInFullClear;
        public bool IsFlush; // Taşmama durumu
    }

    private List<ScoredShape> GenerateAllCandidates(Grid grid)
    {
        List<ScoredShape> validCandidates = new List<ScoredShape>();
        
        foreach (var shape in allShapes)
        {
            var analysis = AnalyzeShape(grid.Cells, grid.Width, grid.Height, shape);
            
            if (analysis.Fits)
            {
                string label;
                int score = CalculateStrictScore(analysis, out label);

                validCandidates.Add(new ScoredShape { 
                    Shape = shape, 
                    Score = score, 
                    Label = label,
                    Mass = analysis.BlockMass,
                    TargetPos = analysis.BestPos 
                });
            }
        }
        return validCandidates.OrderByDescending(x => x.Score).ThenByDescending(x => x.Mass).ToList();
    }

    private ShapeAnalysis AnalyzeShape(bool[,] cells, int w, int h, BlockShapeSO shape)
    {
        ShapeAnalysis result = new ShapeAnalysis { Fits = false, LinesCleared = -1 };
        BlockData data = new BlockData(GetRawMatrixFromSO(shape));
        
        int mass = 0; foreach(var b in data.Matrix) if(b) mass++;
        result.BlockMass = mass;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (CanPlace(cells, w, h, data, x, y)) 
                {
                    if (!result.Fits) result.Fits = true; 

                    var simResult = SimulatePlacement(cells, w, h, data, x, y);
                    int cleared = simResult.linesCleared;
                    bool isFullClear = simResult.isFullClear;
                    bool isFlush = CheckIfFlush(data, x, y, simResult.clearedRows, simResult.clearedCols);
                    float fitScore = CalculateFitScore(cells, w, h, data, x, y);
                    
                    // --- KARŞILAŞTIRMA MANTIĞI ---
                    bool isBetter = false;

                    // 1. Full Clear her zaman en iyisidir
                    if (isFullClear && !result.ResultsInFullClear) isBetter = true;
                    else if (result.ResultsInFullClear) isBetter = false; 
                    
                    // 2. CLEAN KILL (Flush & Satır Silme)
                    // Eğer yeni hamle Flush ise ve eskisi değilse -> YENİ İYİDİR
                    // (Burada satır sayısına bakmıyoruz, Flush olması Perfect Fit'ten bile değerlidir)
                    else if (isFlush && cleared > 0 && !(result.IsFlush && result.LinesCleared > 0)) isBetter = true;
                    else if (!(isFlush && cleared > 0) && (result.IsFlush && result.LinesCleared > 0)) isBetter = false;

                    // 3. Eğer ikisi de Clean Kill ise, daha çok silen kazanır
                    else if (isFlush && cleared > 0 && result.IsFlush && cleared > result.LinesCleared) isBetter = true;

                    // 4. Perfect Fit (Hiç silmeyen veya dağınık silenler arasında en iyi oturan)
                    // Eğer ikisi de Clean Kill DEĞİLSE fit score'a bak
                    else if (fitScore > result.FitScore) isBetter = true;

                    // 5. Eşitlik durumunda satır sayısına bak (Messy Kill durumu)
                    else if (Mathf.Abs(fitScore - result.FitScore) < 0.01f && cleared > result.LinesCleared) isBetter = true;

                    if (isBetter) {
                        result.BestPos = new Vector2Int(x, y);
                        result.LinesCleared = cleared;
                        result.FitScore = fitScore;
                        result.ResultsInFullClear = isFullClear;
                        result.IsFlush = isFlush;
                    }
                }
            }
        }
        return result;
    }

    // --- YENİ PUANLAMA ---
    private int CalculateStrictScore(ShapeAnalysis analysis, out string label)
    {
        // 1. FULL CLEAR
        if (analysis.ResultsInFullClear) { 
            label = "INCREDIBLE"; 
            return TIER_INCREDIBLE + (analysis.BlockMass * 100); 
        }

        // 2. CLEAN KILL (Eski Perfect Flush mantığı buraya taşındı)
        // Şart: Satır silmeli VE Taşmamalı (Flush olmalı)
        if (analysis.IsFlush && analysis.LinesCleared > 0) {
            label = "CLEAN KILL"; // <-- İsim değişti
            // Puanı Perfect Fit'ten yüksek (500k vs 100k)
            return TIER_CLEAN_KILL + (analysis.LinesCleared * 50000) + (analysis.BlockMass * 100);
        }

        // 3. PERFECT FIT (Satır silmiyor veya Flush değil, ama boşluğa cuk oturuyor)
        if (analysis.FitScore >= 0.40f) {
             label = "PERFECT FIT";
             return TIER_PERFECT_FIT + (int)(analysis.FitScore * 5000) + (analysis.BlockMass * 100);
        }

        // 4. MESSY KILL (Satır siliyor ama Flush değil - Dağınık)
        if (analysis.LinesCleared > 0) { 
            label = "MESSY KILL"; 
            // Clean Kill ve Perfect Fit'in altında kalır
            return TIER_MESSY_KILL + (analysis.LinesCleared * 2000) + (analysis.BlockMass * 100); 
        }

        // 5. STANDART
        label = "Fit"; 
        return TIER_FIT + (analysis.BlockMass * 1000) + (int)(analysis.FitScore * 500);
    }

    private bool CheckIfFlush(BlockData data, int px, int py, List<int> clearedRows, List<int> clearedCols)
    {
        if (clearedRows.Count == 0 && clearedCols.Count == 0) return false;

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                bool inClearedRow = clearedRows.Contains(py + y);
                bool inClearedCol = clearedCols.Contains(px + x);

                if (!inClearedRow && !inClearedCol) return false; // Taşma var
            }
        }
        return true; 
    }

    // --- SİMÜLASYON ---
    private (int linesCleared, bool isFullClear, List<int> clearedRows, List<int> clearedCols) SimulatePlacement(bool[,] originalCells, int w, int h, BlockData data, int px, int py)
    {
        bool[,] simGrid = (bool[,])originalCells.Clone();
        for (int i = 0; i < data.Width; i++) for (int j = 0; j < data.Height; j++) if (data.Matrix[i, j]) simGrid[px + i, py + j] = true;

        List<int> r = new List<int>();
        List<int> c = new List<int>();

        for(int y=0; y<h; y++){ bool f=true; for(int x=0; x<w; x++) if(!simGrid[x,y]){f=false; break;} if(f) r.Add(y); } 
        for(int x=0; x<w; x++){ bool f=true; for(int y=0; y<h; y++) if(!simGrid[x,y]){f=false; break;} if(f) c.Add(x); }

        foreach(int y in r) for(int x=0; x<w; x++) simGrid[x,y]=false; 
        foreach(int x in c) for(int y=0; y<h; y++) simGrid[x,y]=false;

        int totalCleared = r.Count + c.Count;
        bool empty = true;
        for(int x=0; x<w; x++) for(int y=0; y<h; y++) if(simGrid[x,y]) { empty=false; break; }

        return (totalCleared, empty, r, c);
    }

    private bool[,] GetRawMatrixFromSO(BlockShapeSO shape)
    {
        int w = shape.width; int h = shape.height;
        bool[,] mat = new bool[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) mat[x, y] = shape.cells[y * w + x];
        return mat;
    }

    private bool CanPlace(bool[,] cells, int w, int h, BlockData data, int px, int py) 
    { 
        if (px + data.Width > w || py + data.Height > h) return false; 
        for (int i = 0; i < data.Width; i++) for (int j = 0; j < data.Height; j++) if (data.Matrix[i, j] && cells[px + i, py + j]) return false; 
        return true; 
    }
    
    public void ShowGhost(BlockShapeSO shape, Vector2Int targetPos) { _ghostShape = shape; _ghostTarget = targetPos; _showGhost = true; }
    public void HideGhost() { _showGhost = false; }

    private void OnDrawGizmos()
    {
        if (!_showGhost || _ghostShape == null || GridManager.Instance == null) return;
        float cellSize = GridManager.Instance.cellSize;
        int w = _ghostShape.width; int h = _ghostShape.height;
        Gizmos.color = new Color(0, 1, 1, 0.6f);
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) if (_ghostShape.cells[y * w + x])
        {
            Vector3 worldPos = GridManager.Instance.CellToWorld(_ghostTarget.x + x, _ghostTarget.y + y);
            worldPos.z = -2f;
            Gizmos.DrawCube(worldPos, Vector3.one * (cellSize * 0.9f));
            Gizmos.DrawWireCube(worldPos, Vector3.one * (cellSize * 0.9f));
        }
    }

    private float CalculateFitScore(bool[,] cells, int w, int h, BlockData data, int px, int py) { float score = 0; int perimeter = 0; for (int lx = 0; lx < data.Width; lx++) { for (int ly = 0; ly < data.Height; ly++) { if (!data.Matrix[lx, ly]) continue; if (lx - 1 < 0 || !data.Matrix[lx - 1, ly]) perimeter++; if (lx + 1 >= data.Width || !data.Matrix[lx + 1, ly]) perimeter++; if (ly - 1 < 0 || !data.Matrix[lx, ly - 1]) perimeter++; if (ly + 1 >= data.Height || !data.Matrix[lx, ly + 1]) perimeter++; Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }; foreach(var d in dirs) { int nx = px + lx + d.x; int ny = py + ly + d.y; if (nx < 0 || nx >= w || ny < 0 || ny >= h || cells[nx, ny]) score += 1.0f; } } } return perimeter == 0 ? 0 : score / perimeter; }
}