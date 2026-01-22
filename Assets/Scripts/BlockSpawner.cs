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

    // --- PUAN BAREMLERİ ---
    private const int TIER_INCREDIBLE   = 1000000; 
    private const int TIER_CLEAN_KILL   = 500000;  
    private const int TIER_PERFECT_FIT  = 100000;  
    private const int TIER_MESSY_KILL   = 50000;   
    private const int TIER_FIT          = 1000;    

    [Header("WarmUp Settings")]
    public Vector2Int warmUpRange = new Vector2Int(10, 20);
    [Range(0, 100)] public int warmUpExtendChance = 30;
    public int warmUpExtendAmount = 2;
    
    [Header("Difficulty Progression (CHAOS)")]
    // Oyun ilerledikçe bu değer 0'dan 1'e doğru artacak
    [Range(0f, 1f)] public float currentChaos = 0f; 
    
    // Her set spawn olduğunda kaos ne kadar artsın?
    public float chaosIncreasePerSet = 0.02f; 
    
    // Kaos 1.0 olduğunda en iyi kaç aday arasından seçim yapılsın?
    // (5 demek: En iyi puanlı parça yerine, en iyi 5 taneden rastgele birini ver demek)
    public int maxChaosSelectionRange = 8; 

    // --- STATE ---
    private int _remainingWarmUpMoves;
    private bool _isWarmUpActive = true;
    
    // --- GHOST ---
    private BlockShapeSO _ghostShape;
    private Vector2Int _ghostTarget;
    private bool _showGhost = false;

    public void StartGame()
    {
        _remainingWarmUpMoves = Random.Range(warmUpRange.x, warmUpRange.y);
        _isWarmUpActive = true;
        currentChaos = 0f; // Zorluğu sıfırla

        Debug.Log($"<color=yellow><b>[GAME START]</b></color> WarmUp: <b>{_remainingWarmUpMoves}</b> Hamle. Kaos: 0%");
        SpawnSet();
    }

    public void ActivateReviveMode() => SpawnSet();

    public void SpawnSet()
    {
        foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        if (GridManager.Instance == null) return;
        Grid grid = GridManager.Instance.LevelGrid;
        
        // 1. ADAYLARI OLUŞTUR VE PUANLA (En iyiden en kötüye sıralı gelir)
        List<ScoredShape> candidates = GenerateAllCandidates(grid);

        if (candidates.Count == 0)
        {
             Debug.Log("<color=red>[GAME OVER]</color> Hiçbir parça sığmıyor!");
             GameManager.Instance.TriggerGameOver();
             return;
        }

        List<ScoredShape> finalPicks = new List<ScoredShape>();
        HashSet<BlockShapeSO> usedShapes = new HashSet<BlockShapeSO>();

        // 2. KAOS AYARI (SEÇİM ARALIĞINI BELİRLE)
        // Eğer WarmUp ise range = 1 (Sadece en iyisi).
        // Değilse Kaos'a göre range genişler (1 ile maxChaosSelectionRange arası).
        int selectionRange = 1;
        
        if (!_isWarmUpActive)
        {
            // Kaos arttıkça seçim havuzu genişler.
            // Örn: Kaos 0.5 ise ve Max 8 ise -> Range 4 olur. (İlk 4 adaydan biri seçilir)
            selectionRange = Mathf.RoundToInt(Mathf.Lerp(1, maxChaosSelectionRange, currentChaos));
            
            // Havuz aday sayısını geçemez
            if (selectionRange > candidates.Count) selectionRange = candidates.Count;
            
            // Her set spawn olduğunda zorluğu biraz artır (Max 1.0)
            currentChaos = Mathf.Clamp01(currentChaos + chaosIncreasePerSet);
            
            Debug.Log($"<color=red>[DIFFICULTY]</color> Kaos: {currentChaos:F2} | Seçim Havuzu: İlk {selectionRange} aday");
        }

        // 3. SEÇİM YAP
        for(int i=0; i<slots.Length; i++) 
        {
            ScoredShape pick = null;
            
            // --- YENİ SEÇİM MANTIĞI ---
            // Eskiden: candidates listesinde sırayla geziyorduk (0, 1, 2...).
            // Şimdi: Belirlenen "Range" içinden RASTGELE birini deneyeceğiz.
            
            // Havuzdan (0 ile selectionRange arası) rastgele denemeler yap
            // Sonsuz döngüye girmemek için max deneme sayısı koyuyoruz
            int attempts = 10; 
            while(attempts > 0)
            {
                int randomIndex = Random.Range(0, selectionRange);
                // Listenin dışına taşma kontrolü (Range list boyundan büyükse diye)
                if (randomIndex >= candidates.Count) randomIndex = 0; 
                
                var potentialCand = candidates[randomIndex];
                
                if (!usedShapes.Contains(potentialCand.Shape))
                {
                    pick = potentialCand;
                    break; // Bulduk!
                }
                attempts--;
            }

            // Eğer rastgele seçimle bulamadıysak (hepsi kullanılmışsa),
            // Klasik yöntemle (sırayla) boşta kalanı bul
            if (pick == null)
            {
                foreach(var cand in candidates) { if(!usedShapes.Contains(cand.Shape)) { pick = cand; break; } }
            }
            
            // Hiç çare kalmadıysa en iyisini ver
            if (pick == null && candidates.Count > 0) pick = candidates[0];

            if (pick != null)
            {
                finalPicks.Add(pick);
                usedShapes.Add(pick.Shape);
            }
        }

        // 4. OLUŞTUR
        for(int i=0; i<finalPicks.Count; i++) 
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(finalPicks[i].Shape, finalPicks[i].TargetPos);
            _activeBlocks.Add(b);
        }
    }

    public void OnBlockPlaced(DraggableBlock b) 
    { 
        _activeBlocks.Remove(b); 
        
        if (_isWarmUpActive)
        {
            _remainingWarmUpMoves--;
            if (_remainingWarmUpMoves <= 0) {
                _isWarmUpActive = false;
                Debug.Log($"<color=cyan><b>[WARM UP ENDED]</b></color> Kaos başlıyor...");
            }
            else
            {
                Debug.Log($"<color=orange>[WARM UP]</color> Kalan: {_remainingWarmUpMoves}");
            }
        }
        
        if (_activeBlocks.Count == 0) SpawnSet(); 
        else StartCoroutine(CheckOver()); 
    }
    
    public void OnLinesCleared(int linesCount) 
    { 
        if (_isWarmUpActive && linesCount > 0)
        {
            if (Random.Range(0, 100) < warmUpExtendChance)
            {
                _remainingWarmUpMoves += warmUpExtendAmount;
                Debug.Log($"<color=green>[LUCKY!]</color> WarmUp Uzadı (+{warmUpExtendAmount})");
            }
        }
    }
    
    // ... (Geri kalan AnalyzeShape, CalculateStrictScore vb. kodları AYNI kalacak) ...
    // ... Sadece üst tarafı değiştirdik ...
    
    private IEnumerator CheckOver() { yield return new WaitForEndOfFrame(); CheckGameOver(); }
    private void CheckGameOver() { if (_activeBlocks.Count == 0) return; foreach (var b in _activeBlocks) if (GridManager.Instance.CanFitAnywhere(b.GetData())) return; GameManager.Instance.TriggerGameOver(); }
    private class ScoredShape { public BlockShapeSO Shape; public int Score; public string Label; public int Mass; public Vector2Int TargetPos; }
    private struct ShapeAnalysis { public bool Fits; public Vector2Int BestPos; public int LinesCleared; public float FitScore; public int BlockMass; public bool ResultsInFullClear; public bool IsFlush; }
    private List<ScoredShape> GenerateAllCandidates(Grid grid) { List<ScoredShape> validCandidates = new List<ScoredShape>(); foreach (var shape in allShapes) { var analysis = AnalyzeShape(grid.Cells, grid.Width, grid.Height, shape); if (analysis.Fits) { string label; int score = CalculateStrictScore(analysis, out label); validCandidates.Add(new ScoredShape { Shape = shape, Score = score, Label = label, Mass = analysis.BlockMass, TargetPos = analysis.BestPos }); } } return validCandidates.OrderByDescending(x => x.Score).ThenByDescending(x => x.Mass).ToList(); }
    private ShapeAnalysis AnalyzeShape(bool[,] cells, int w, int h, BlockShapeSO shape) { ShapeAnalysis result = new ShapeAnalysis { Fits = false, LinesCleared = -1 }; BlockData data = new BlockData(GetRawMatrixFromSO(shape)); int mass = 0; foreach(var b in data.Matrix) if(b) mass++; result.BlockMass = mass; for (int x = 0; x < w; x++) { for (int y = 0; y < h; y++) { if (CanPlace(cells, w, h, data, x, y)) { if (!result.Fits) result.Fits = true; var simResult = SimulatePlacement(cells, w, h, data, x, y); int cleared = simResult.linesCleared; bool isFullClear = simResult.isFullClear; bool isFlush = CheckIfFlush(data, x, y, simResult.clearedRows, simResult.clearedCols); float fitScore = CalculateFitScore(cells, w, h, data, x, y); bool isBetter = false; if (isFullClear && !result.ResultsInFullClear) isBetter = true; else if (result.ResultsInFullClear) isBetter = false; else if (isFlush && cleared > 0 && !(result.IsFlush && result.LinesCleared > 0)) isBetter = true; else if (!(isFlush && cleared > 0) && (result.IsFlush && result.LinesCleared > 0)) isBetter = false; else if (isFlush && cleared > 0 && result.IsFlush && cleared > result.LinesCleared) isBetter = true; else if (fitScore > result.FitScore) isBetter = true; else if (Mathf.Abs(fitScore - result.FitScore) < 0.01f && cleared > result.LinesCleared) isBetter = true; if (isBetter) { result.BestPos = new Vector2Int(x, y); result.LinesCleared = cleared; result.FitScore = fitScore; result.ResultsInFullClear = isFullClear; result.IsFlush = isFlush; } } } } return result; }
    private int CalculateStrictScore(ShapeAnalysis analysis, out string label) { if (analysis.ResultsInFullClear) { label = "INCREDIBLE"; return TIER_INCREDIBLE + (analysis.BlockMass * 100); } if (analysis.IsFlush && analysis.LinesCleared > 0) { label = "CLEAN KILL"; return TIER_CLEAN_KILL + (analysis.LinesCleared * 50000) + (analysis.BlockMass * 100); } if (analysis.FitScore >= 0.4f) { label = "PERFECT FIT"; return TIER_PERFECT_FIT + (int)(analysis.FitScore * 5000) + (analysis.BlockMass * 100); } if (analysis.LinesCleared > 0) { label = "MESSY KILL"; return TIER_MESSY_KILL + (analysis.LinesCleared * 2000) + (analysis.BlockMass * 100); } label = "Fit"; return TIER_FIT + (analysis.BlockMass * 1000) + (int)(analysis.FitScore * 500); }
    private bool CheckIfFlush(BlockData data, int px, int py, List<int> clearedRows, List<int> clearedCols) { if (clearedRows.Count == 0 && clearedCols.Count == 0) return false; for (int x = 0; x < data.Width; x++) { for (int y = 0; y < data.Height; y++) { if (!data.Matrix[x, y]) continue; if (!clearedRows.Contains(py + y) && !clearedCols.Contains(px + x)) return false; } } return true; }
    private (int linesCleared, bool isFullClear, List<int> clearedRows, List<int> clearedCols) SimulatePlacement(bool[,] originalCells, int w, int h, BlockData data, int px, int py) { bool[,] simGrid = (bool[,])originalCells.Clone(); for (int i = 0; i < data.Width; i++) for (int j = 0; j < data.Height; j++) if (data.Matrix[i, j]) simGrid[px + i, py + j] = true; List<int> r = new List<int>(); List<int> c = new List<int>(); for(int y=0; y<h; y++){ bool f=true; for(int x=0; x<w; x++) if(!simGrid[x,y]){f=false; break;} if(f) r.Add(y); } for(int x=0; x<w; x++){ bool f=true; for(int y=0; y<h; y++) if(!simGrid[x,y]){f=false; break;} if(f) c.Add(x); } foreach(int y in r) for(int x=0; x<w; x++) simGrid[x,y]=false; foreach(int x in c) for(int y=0; y<h; y++) simGrid[x,y]=false; bool empty = true; for(int x=0; x<w; x++) for(int y=0; y<h; y++) if(simGrid[x,y]) { empty=false; break; } return (r.Count + c.Count, empty, r, c); }
    private bool[,] GetRawMatrixFromSO(BlockShapeSO shape) { int w = shape.width; int h = shape.height; bool[,] mat = new bool[w, h]; for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) mat[x, y] = shape.cells[y * w + x]; return mat; }
    private bool CanPlace(bool[,] cells, int w, int h, BlockData data, int px, int py) { if (px + data.Width > w || py + data.Height > h) return false; for (int i = 0; i < data.Width; i++) for (int j = 0; j < data.Height; j++) if (data.Matrix[i, j] && cells[px + i, py + j]) return false; return true; }
    public void ShowGhost(BlockShapeSO shape, Vector2Int targetPos) { _ghostShape = shape; _ghostTarget = targetPos; _showGhost = true; }
    public void HideGhost() { _showGhost = false; }
    private void OnDrawGizmos() { if (!_showGhost || _ghostShape == null || GridManager.Instance == null) return; float cellSize = GridManager.Instance.cellSize; int w = _ghostShape.width; int h = _ghostShape.height; Gizmos.color = new Color(0, 1, 1, 0.6f); for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) if (_ghostShape.cells[y * w + x]) { Vector3 worldPos = GridManager.Instance.CellToWorld(_ghostTarget.x + x, _ghostTarget.y + y); worldPos.z = -2f; Gizmos.DrawCube(worldPos, Vector3.one * (cellSize * 0.9f)); Gizmos.DrawWireCube(worldPos, Vector3.one * (cellSize * 0.9f)); } }
    private float CalculateFitScore(bool[,] cells, int w, int h, BlockData data, int px, int py) { float score = 0; int perimeter = 0; for (int lx = 0; lx < data.Width; lx++) { for (int ly = 0; ly < data.Height; ly++) { if (!data.Matrix[lx, ly]) continue; if (lx - 1 < 0 || !data.Matrix[lx - 1, ly]) perimeter++; if (lx + 1 >= data.Width || !data.Matrix[lx + 1, ly]) perimeter++; if (ly - 1 < 0 || !data.Matrix[lx, ly - 1]) perimeter++; if (ly + 1 >= data.Height || !data.Matrix[lx, ly + 1]) perimeter++; Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }; foreach(var d in dirs) { int nx = px + lx + d.x; int ny = py + ly + d.y; if (nx < 0 || nx >= w || ny < 0 || ny >= h || cells[nx, ny]) score += 1.0f; } } } return perimeter == 0 ? 0 : score / perimeter; }
}