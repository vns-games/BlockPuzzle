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

    // --- YENİ EKLENEN: SON SET HAFIZASI ---
    private List<BlockShapeSO> _lastSpawnedSet = new List<BlockShapeSO>();
    // -------------------------------------

    // --- PUAN BAREMLERİ ---
    private const int TIER_INCREDIBLE = 1000000;
    private const int TIER_CLEAN_KILL = 500000;
    private const int TIER_PERFECT_FIT = 100000;
    private const int TIER_MESSY_KILL = 50000;
    private const int TIER_FIT = 1000;

    [Header("WarmUp Settings")]
    public Vector2Int warmUpRange = new Vector2Int(10, 20);
    public Vector2Int warmUpExtendChanceRange = new Vector2Int(20, 50);

    [Header("Difficulty Progression (CHAOS)")]
    [Range(0f, 3f)] public float currentChaos = 0f;
    public float maxChaosCap = 3.0f;
    public Vector2 chaosStepRange = new Vector2(0.02f, 0.05f);
    public int baseSelectionRange = 6;
    public int skipBestPerChaosPoint = 3;

    [Header("Chaos Recovery (Luck)")]
    [Range(0, 100)] public int chaosReduceChance = 25;
    public Vector2 chaosReduceAmountRange = new Vector2(0.1f, 0.4f);

    [Header("Threshold Settings")]
    public float startFitThreshold = 0.40f;
    public float minFitThreshold = 0.15f;
    public float thresholdDecaySpeed = 0.005f;

    // --- STATE ---
    private int _remainingWarmUpMoves;
    private bool _isWarmUpActive = true;
    private float _currentFitThreshold;

    // --- GHOST ---
    private BlockShapeSO _ghostShape;
    private Vector2Int _ghostTarget;
    private bool _showGhost = false;

    public void StartGame()
    {
        _remainingWarmUpMoves = Random.Range(warmUpRange.x, warmUpRange.y);
        _isWarmUpActive = true;

        currentChaos = 0f;
        _currentFitThreshold = startFitThreshold;

        // Oyuna başlarken hafızayı temizle
        _lastSpawnedSet.Clear();

        Debug.Log($"<color=yellow><b>[GAME START]</b></color> WarmUp: <b>{_remainingWarmUpMoves}</b> Hamle. Kaos: 0%");
        SpawnSet();
    }

    public void ActivateReviveMode() => SpawnSet();

    public void SpawnSet()
    {
        foreach (var b in _activeBlocks)
            if (b)
                Destroy(b.gameObject);
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

        // --- ZORLUK HESAPLAMALARI ---
        int selectionPoolSize = 1;
        int skipBestCount = 0;

        if (!_isWarmUpActive)
        {
            float randomStep = Random.Range(chaosStepRange.x, chaosStepRange.y);
            currentChaos = Mathf.Clamp(currentChaos + randomStep, 0f, maxChaosCap);

            float normalizedChaosForPool = Mathf.Clamp01(currentChaos);
            selectionPoolSize = Mathf.RoundToInt(Mathf.Lerp(1, baseSelectionRange, normalizedChaosForPool));

            if (currentChaos > 1.0f)
            {
                float excessChaos = currentChaos - 1.0f;
                skipBestCount = Mathf.FloorToInt(excessChaos * skipBestPerChaosPoint);
            }

            Debug.Log($"<color=red>[DIFFICULTY]</color> Kaos: <b>{currentChaos:F2}</b> | Havuz: {selectionPoolSize} | Yasaklı: {skipBestCount}");
        }
        else
        {
            selectionPoolSize = 1;
            skipBestCount = 0;
        }

        // --- SEÇİM DÖNGÜSÜ ---
        for(int i = 0; i < slots.Length; i++)
        {
            ScoredShape pick = null;
            int attempts = 15;

            while(attempts > 0)
            {
                int minIndex = skipBestCount;
                int maxIndex = skipBestCount + selectionPoolSize;

                if (minIndex >= candidates.Count) minIndex = candidates.Count - 1;
                if (maxIndex > candidates.Count) maxIndex = candidates.Count;

                if (minIndex >= maxIndex)
                {
                    minIndex = candidates.Count - 1;
                    maxIndex = candidates.Count;
                }

                int randomIndex = Random.Range(minIndex, maxIndex);
                var potentialCand = candidates[randomIndex];

                if (!usedShapes.Contains(potentialCand.Shape))
                {
                    pick = potentialCand;
                    break;
                }
                attempts--;
            }

            if (pick == null)
            {
                for(int k = skipBestCount; k < candidates.Count; k++)
                {
                    if (!usedShapes.Contains(candidates[k].Shape))
                    {
                        pick = candidates[k];
                        break;
                    }
                }
            }

            if (pick == null && candidates.Count > 0) pick = candidates[candidates.Count - 1];

            if (pick != null)
            {
                finalPicks.Add(pick);
                usedShapes.Add(pick.Shape);
            }
        }

        // ==========================================================
        // YENİ BÖLÜM: SONSUZ DÖNGÜ ENGELLEYİCİ (INFINITY GLITCH FIX)
        // ==========================================================

        bool isExactMatch = false;

        // 1. Önceki set ile bu setin boyutu aynı mı? (Genelde 3'tür ama kontrol edelim)
        if (_lastSpawnedSet.Count > 0 && _lastSpawnedSet.Count == finalPicks.Count)
        {
            isExactMatch = true;
            for(int i = 0; i < finalPicks.Count; i++)
            {
                // Eğer herhangi bir parça farklıysa, bu set farklıdır.
                if (finalPicks[i].Shape != _lastSpawnedSet[i])
                {
                    isExactMatch = false;
                    break;
                }
            }
        }

        // 2. Eğer birebir aynı set geldiyse ve elimizde başka alternatif varsa
        if (isExactMatch && candidates.Count > 1)
        {
            // Debug.Log("<color=yellow>Sonsuz Döngü Yakalandı! Set zorla değiştiriliyor.</color>");

            // Son slotu değiştirelim
            int lastSlotIndex = finalPicks.Count - 1;
            BlockShapeSO currentShape = finalPicks[lastSlotIndex].Shape;

            // Candidates listesinden şu ankiyle AYNI OLMAYAN rastgele bir tane bul
            ScoredShape newPick = null;
            int attempts = 15;
            while(attempts > 0)
            {
                var candidate = candidates[Random.Range(0, candidates.Count)];
                if (candidate.Shape != currentShape) // Farklı bir şekil bulduk
                {
                    newPick = candidate;
                    break;
                }
                attempts--;
            }

            // Eğer geçerli bir değişim bulduysak uygula
            if (newPick != null)
            {
                finalPicks[lastSlotIndex] = newPick;
            }
        }

        // 3. Bu seti hafızaya kaydet
        _lastSpawnedSet.Clear();
        foreach (var pick in finalPicks)
        {
            _lastSpawnedSet.Add(pick.Shape);
        }
        // ==========================================================
        // BİTİŞ: SONSUZ DÖNGÜ ENGELLEYİCİ
        // ==========================================================

        // --- INSTANTIATE ---
        for(int i = 0; i < finalPicks.Count; i++)
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

            if (_remainingWarmUpMoves <= 0)
            {
                _isWarmUpActive = false;
                Debug.Log($"<color=cyan><b>[WARM UP ENDED]</b></color> Isınma bitti! Kaos başlıyor!");
            }
            else
            {
                Debug.Log($"<color=orange>[WARM UP]</color> Kalan: <b>{_remainingWarmUpMoves}</b>");
            }
        }
        else
        {
            if (_currentFitThreshold > minFitThreshold) _currentFitThreshold -= thresholdDecaySpeed;
        }

        if (_activeBlocks.Count == 0) SpawnSet();
        else StartCoroutine(CheckOver());
    }

    public void OnLinesCleared(int linesCount)
    {
        if (linesCount <= 0) return;

        if (_isWarmUpActive)
        {
            int currentChance = Random.Range(warmUpExtendChanceRange.x, warmUpExtendChanceRange.y);
            if (Random.Range(0, 100) < currentChance)
            {
                int extension = Random.Range(1, 3);
                _remainingWarmUpMoves += extension;
                Debug.Log($"<color=green><b>[LUCKY!]</b></color> WarmUp Uzadı (+{extension})");
            }
        }
        else
        {
            if (currentChaos > 0)
            {
                if (Random.Range(0, 100) < chaosReduceChance)
                {
                    float reductionAmount = Random.Range(chaosReduceAmountRange.x, chaosReduceAmountRange.y);

                    float oldChaos = currentChaos;
                    currentChaos -= reductionAmount;
                    if (currentChaos < 0) currentChaos = 0;

                    Debug.Log($"<color=lime><b>[RECOVERY!]</b></color> Kaos Düştü (-{reductionAmount:F2}): {oldChaos:F2} -> <b>{currentChaos:F2}</b>");
                }
            }
        }
    }

    // ... ALTTAKİ KODLAR AYNEN KALDI ...
    private IEnumerator CheckOver()
    {
        yield return new WaitForEndOfFrame();
        CheckGameOver();
    }
    private void CheckGameOver()
    {
        if (_activeBlocks.Count == 0) return;
        foreach (var b in _activeBlocks)
            if (GridManager.Instance.CanFitAnywhere(b.GetData()))
                return;
        GameManager.Instance.TriggerGameOver();
    }
    private class ScoredShape
    {
        public BlockShapeSO Shape;
        public int Score;
        public string Label;
        public int Mass;
        public Vector2Int TargetPos;
    }
    private struct ShapeAnalysis
    {
        public bool Fits;
        public Vector2Int BestPos;
        public int LinesCleared;
        public float FitScore;
        public int BlockMass;
        public bool ResultsInFullClear;
        public bool IsFlush;
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
                validCandidates.Add(new ScoredShape
                {
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
        ShapeAnalysis result = new ShapeAnalysis
        {
            Fits = false,
            LinesCleared = -1
        };
        BlockData data = new BlockData(GetRawMatrixFromSO(shape));
        int mass = 0;
        foreach (var b in data.Matrix)
            if (b)
                mass++;
        result.BlockMass = mass;
        for(int x = 0; x < w; x++)
        {
            for(int y = 0; y < h; y++)
            {
                if (CanPlace(cells, w, h, data, x, y))
                {
                    if (!result.Fits) result.Fits = true;
                    var simResult = SimulatePlacement(cells, w, h, data, x, y);
                    int cleared = simResult.linesCleared;
                    bool isFullClear = simResult.isFullClear;
                    bool isFlush = CheckIfFlush(data, x, y, simResult.clearedRows, simResult.clearedCols);
                    float fitScore = CalculateFitScore(cells, w, h, data, x, y);
                    bool isBetter = false;
                    if (isFullClear && !result.ResultsInFullClear) isBetter = true;
                    else if (result.ResultsInFullClear) isBetter = false;
                    else if (isFlush && cleared > 0 && !(result.IsFlush && result.LinesCleared > 0)) isBetter = true;
                    else if (!(isFlush && cleared > 0) && (result.IsFlush && result.LinesCleared > 0)) isBetter = false;
                    else if (isFlush && cleared > 0 && result.IsFlush && cleared > result.LinesCleared) isBetter = true;
                    else if (fitScore > result.FitScore) isBetter = true;
                    else if (Mathf.Abs(fitScore - result.FitScore) < 0.01f && cleared > result.LinesCleared) isBetter = true;
                    if (isBetter)
                    {
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
    private int CalculateStrictScore(ShapeAnalysis analysis, out string label)
    {
        if (analysis.ResultsInFullClear)
        {
            label = "INCREDIBLE";
            return TIER_INCREDIBLE + (analysis.BlockMass * 100);
        }
        if (analysis.IsFlush && analysis.LinesCleared > 0)
        {
            label = "CLEAN KILL";
            return TIER_CLEAN_KILL + (analysis.LinesCleared * 50000) + (analysis.BlockMass * 100);
        }
        if (analysis.FitScore >= _currentFitThreshold)
        {
            label = "PERFECT FIT";
            return TIER_PERFECT_FIT + (int)(analysis.FitScore * 5000) + (analysis.BlockMass * 100);
        }
        if (analysis.LinesCleared > 0)
        {
            label = "MESSY KILL";
            return TIER_MESSY_KILL + (analysis.LinesCleared * 2000) + (analysis.BlockMass * 100);
        }
        label = "Fit";
        return TIER_FIT + (analysis.BlockMass * 1000) + (int)(analysis.FitScore * 500);
    }
    private bool CheckIfFlush(BlockData data, int px, int py, List<int> clearedRows, List<int> clearedCols)
    {
        if (clearedRows.Count == 0 && clearedCols.Count == 0) return false;
        for(int x = 0; x < data.Width; x++)
        {
            for(int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                if (!clearedRows.Contains(py + y) && !clearedCols.Contains(px + x)) return false;
            }
        }
        return true;
    }
    private (int linesCleared, bool isFullClear, List<int> clearedRows, List<int> clearedCols) SimulatePlacement(bool[,] originalCells, int w, int h, BlockData data, int px, int py)
    {
        bool[,] simGrid = (bool[,])originalCells.Clone();
        for(int i = 0; i < data.Width; i++)
            for(int j = 0; j < data.Height; j++)
                if (data.Matrix[i, j])
                    simGrid[px + i, py + j] = true;
        List<int> r = new List<int>();
        List<int> c = new List<int>();
        for(int y = 0; y < h; y++)
        {
            bool f = true;
            for(int x = 0; x < w; x++)
                if (!simGrid[x, y])
                {
                    f = false;
                    break;
                }
            if (f) r.Add(y);
        }
        for(int x = 0; x < w; x++)
        {
            bool f = true;
            for(int y = 0; y < h; y++)
                if (!simGrid[x, y])
                {
                    f = false;
                    break;
                }
            if (f) c.Add(x);
        }
        foreach (int y in r)
            for(int x = 0; x < w; x++)
                simGrid[x, y] = false;
        foreach (int x in c)
            for(int y = 0; y < h; y++)
                simGrid[x, y] = false;
        bool empty = true;
        for(int x = 0; x < w; x++)
            for(int y = 0; y < h; y++)
                if (simGrid[x, y])
                {
                    empty = false;
                    break;
                }
        return (r.Count + c.Count, empty, r, c);
    }
    private bool[,] GetRawMatrixFromSO(BlockShapeSO shape)
    {
        int w = shape.width;
        int h = shape.height;
        bool[,] mat = new bool[w, h];
        for(int x = 0; x < w; x++)
            for(int y = 0; y < h; y++)
                mat[x, y] = shape.cells[y * w + x];
        return mat;
    }
    private bool CanPlace(bool[,] cells, int w, int h, BlockData data, int px, int py)
    {
        if (px + data.Width > w || py + data.Height > h) return false;
        for(int i = 0; i < data.Width; i++)
            for(int j = 0; j < data.Height; j++)
                if (data.Matrix[i, j] && cells[px + i, py + j])
                    return false;
        return true;
    }
    public void ShowGhost(BlockShapeSO shape, Vector2Int targetPos)
    {
        _ghostShape = shape;
        _ghostTarget = targetPos;
        _showGhost = true;
    }
    public void HideGhost() { _showGhost = false; }
    private void OnDrawGizmos()
    {
        if (!_showGhost || _ghostShape == null || GridManager.Instance == null) return;
        float cellSize = GridManager.Instance.cellSize;
        int w = _ghostShape.width;
        int h = _ghostShape.height;
        Gizmos.color = new Color(0, 1, 1, 0.6f);
        for(int x = 0; x < w; x++)
            for(int y = 0; y < h; y++)
                if (_ghostShape.cells[y * w + x])
                {
                    Vector3 worldPos = GridManager.Instance.CellToWorld(_ghostTarget.x + x, _ghostTarget.y + y);
                    worldPos.z = -2f;
                    Gizmos.DrawCube(worldPos, Vector3.one * (cellSize * 0.9f));
                    Gizmos.DrawWireCube(worldPos, Vector3.one * (cellSize * 0.9f));
                }
    }
    private float CalculateFitScore(bool[,] cells, int w, int h, BlockData data, int px, int py)
    {
        float score = 0;
        int perimeter = 0;
        for(int lx = 0; lx < data.Width; lx++)
        {
            for(int ly = 0; ly < data.Height; ly++)
            {
                if (!data.Matrix[lx, ly]) continue;
                if (lx - 1 < 0 || !data.Matrix[lx - 1, ly]) perimeter++;
                if (lx + 1 >= data.Width || !data.Matrix[lx + 1, ly]) perimeter++;
                if (ly - 1 < 0 || !data.Matrix[lx, ly - 1]) perimeter++;
                if (ly + 1 >= data.Height || !data.Matrix[lx, ly + 1]) perimeter++;
                Vector2Int[] dirs =
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
                };
                foreach (var d in dirs)
                {
                    int nx = px + lx + d.x;
                    int ny = py + ly + d.y;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h || cells[nx, ny]) score += 1.0f;
                }
            }
        }
        return perimeter == 0 ? 0 : score / perimeter;
    }
}