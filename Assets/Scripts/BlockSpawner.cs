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

    // --- PUAN BAREMLERİ (HIYERARŞİ ASLA BOZULMAZ) ---
    private const int TIER_INCREDIBLE   = 1000000; // 1. Öncelik
    private const int TIER_MEGA_KILL    = 500000;  // 2. Öncelik
    private const int TIER_PERFECT_FIT  = 100000;  // 3. Öncelik
    private const int TIER_CLEAN_KILL   = 50000;   // 4. Öncelik
    private const int TIER_FIT          = 1000;    // 5. Öncelik

    [Header("Settings")]
    public int rewardMovesPerLine = 2;
    private int _targetWarmUpMoves = 0;

    public void StartGame()
    {
        DebugShapeData();
        Debug.Log($"<color=yellow>[START]</color> OYUN BAŞLADI.");
        SpawnSet();
    }

    public void ActivateReviveMode() => SpawnSet();

    public void SpawnSet()
    {
        foreach (var b in _activeBlocks) if (b) Destroy(b.gameObject);
        _activeBlocks.Clear();

        Grid grid = GridManager.Instance.LevelGrid;
        
        // --- 1. TÜM OLASILIKLARI HESAPLA ---
        List<ScoredShape> candidates = GenerateAllCandidates(grid);

        // --- 2. GAME OVER KONTROLÜ ---
        if (candidates.Count == 0)
        {
             Debug.LogError("<color=red>[GAME OVER]</color> Hiçbir parça sığmıyor!");
             GameManager.Instance.TriggerGameOver();
             return;
        }

        // --- 3. SEÇİM YAP VE OLUŞTUR ---
        // En yüksek puanlıları seç (Her slot için sıradaki en iyiyi al)
        List<BlockShapeSO> finalBatch = new List<BlockShapeSO>();
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

            // Yedek plan (Çok az çeşit varsa en iyiyi tekrarla)
            if (pick == null && candidates.Count > 0) pick = candidates[0];

            if (pick != null)
            {
                finalBatch.Add(pick.Shape);
                usedShapes.Add(pick.Shape);

                // LOGLAMA
                string color = "white";
                if (pick.Score >= TIER_INCREDIBLE) color = "magenta";
                else if (pick.Score >= TIER_MEGA_KILL) color = "green";
                else if (pick.Score >= TIER_PERFECT_FIT) color = "cyan";
                else if (pick.Score >= TIER_CLEAN_KILL) color = "yellow";

                BlockData d = new BlockData(pick.Shape.ToMatrix().Trim());
                Debug.Log($"<color={color}>[SLOT {i} SEÇİLDİ]</color> <b>{pick.Shape.name}</b> ({d.Width}x{d.Height}) | {pick.Label} | Puan: {pick.Score}");
            }
        }

        // Objeleri Yarat
        for(int i=0; i<finalBatch.Count; i++) 
        {
            var b = Instantiate(blockPrefab, slots[i].position, Quaternion.identity);
            b.Initialize(finalBatch[i]);
            _activeBlocks.Add(b);
        }
    }

    // ======================================================================================
    //  ADAY OLUŞTURUCU (CANDIDATE GENERATOR)
    // ======================================================================================
    private List<ScoredShape> GenerateAllCandidates(Grid grid)
    {
        List<ScoredShape> validCandidates = new List<ScoredShape>();
        
        // Debug için string oluşturucu
        System.Text.StringBuilder debugLog = new System.Text.StringBuilder();
        debugLog.AppendLine("<color=orange>--- ADAY LİSTESİ (Sığanlar) ---</color>");

        foreach (var shape in allShapes)
        {
            // 1. Analiz Et
            var analysis = AnalyzeShape(grid.Cells, grid.Width, grid.Height, shape);
            
            // 2. SADECE SIĞANLARI AL
            if (analysis.Fits)
            {
                string label;
                int score = CalculateStrictScore(analysis, out label);

                validCandidates.Add(new ScoredShape { 
                    Shape = shape, 
                    Score = score, 
                    Label = label,
                    Mass = analysis.BlockMass
                });

                debugLog.AppendLine($"- {shape.name}: {label} ({score})");
            }
        }

        // Eğer liste boşsa loga yaz
        if (validCandidates.Count == 0) debugLog.AppendLine("HİÇBİR PARÇA SIĞMADI!");
        
        // Debug.Log(debugLog.ToString()); // Konsolu kirletmemek için kapalı, gerekirse aç.

        // 3. PUANA GÖRE SIRALA (En büyük en başa)
        return validCandidates.OrderByDescending(x => x.Score).ThenByDescending(x => x.Mass).ToList();
    }

    // ======================================================================================
    //  KESİN PUANLAMA (HIYERARŞİ)
    // ======================================================================================
    private int CalculateStrictScore(ShapeAnalysis analysis, out string label)
    {
        // 1. INCREDIBLE (Full Clear) -> 1 Milyon Puan
        if (analysis.ResultsInFullClear) 
        { 
            label = "INCREDIBLE"; 
            return TIER_INCREDIBLE + (analysis.BlockMass * 1000); 
        }
        
        // 2. MEGA KILL (3+ Satır) -> 500 Bin Puan
        if (analysis.LinesCleared >= 3) 
        { 
            label = "MEGA KILL"; 
            return TIER_MEGA_KILL + (analysis.LinesCleared * 5000) + (analysis.BlockMass * 1000); 
        }
        
        // 3. PERFECT FIT (İyi Oturma) -> 100 Bin Puan
        // Not: Satır silse bile Mega Kill (3 satır) değilse buraya düşebilir.
        // Ama Clean Kill (50k) puanından yüksek olduğu için önceliklidir.
        if (analysis.FitScore >= 0.40f) 
        { 
            label = "PERFECT FIT"; 
            int s = TIER_PERFECT_FIT + (int)(analysis.FitScore * 5000) + (analysis.BlockMass * 1000);
            if(analysis.LinesCleared > 0) s += 20000; // Bonus
            return s;
        }
        
        // 4. CLEAN KILL (1-2 Satır) -> 50 Bin Puan
        if (analysis.LinesCleared > 0) 
        { 
            label = "CLEAN KILL"; 
            return TIER_CLEAN_KILL + (analysis.LinesCleared * 2000) + (analysis.BlockMass * 1000); 
        }
        
        // 5. STANDARD (Sığma) -> 1000 Puan
        label = "Standard";
        return TIER_FIT + (analysis.BlockMass * 1000);
    }

    private class ScoredShape { public BlockShapeSO Shape; public int Score; public string Label; public int Mass; }

    // ======================================================================================
    //  ANALİZ MOTORU
    // ======================================================================================
    private struct ShapeAnalysis { public bool Fits; public Vector2Int BestPos; public int LinesCleared; public float FitScore; public int BlockMass; public bool ResultsInFullClear; }

    private ShapeAnalysis AnalyzeShape(bool[,] cells, int w, int h, BlockShapeSO shape)
    {
        ShapeAnalysis result = new ShapeAnalysis { Fits = false, LinesCleared = -1 };
        BlockData data = new BlockData(shape.ToMatrix().Trim());
        
        int mass = 0; foreach(var b in data.Matrix) if(b) mass++;
        result.BlockMass = mass;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (CanPlace(cells, w, h, data, x, y)) 
                {
                    if (!result.Fits) result.Fits = true; 

                    int cleared = SimulateAndCountClearedLines(cells, w, h, data, x, y);
                    bool isFullClear = IsBoardEmptyAfterPlace(cells, w, h, data, x, y);
                    float fitScore = CalculateFitScore(cells, w, h, data, x, y);
                    
                    // En iyi pozisyonu seç
                    bool isBetter = false;
                    if (isFullClear && !result.ResultsInFullClear) isBetter = true; // Full Clear her zaman kraldır
                    else if (result.ResultsInFullClear) isBetter = false; 
                    else if (cleared > result.LinesCleared) isBetter = true; // Çok satır silen iyidir
                    else if (cleared == result.LinesCleared && fitScore > result.FitScore) isBetter = true; // Eşitse iyi oturan iyidir

                    if (isBetter) {
                        result.BestPos = new Vector2Int(x, y);
                        result.LinesCleared = cleared;
                        result.FitScore = fitScore;
                        result.ResultsInFullClear = isFullClear;
                    }
                }
            }
        }
        return result;
    }

    // --- YARDIMCILAR ---
    private int SimulateAndCountClearedLines(bool[,] originalCells, int w, int h, BlockData data, int px, int py)
    {
        bool[,] simGrid = (bool[,])originalCells.Clone();
        for (int i = 0; i < data.Width; i++) for (int j = 0; j < data.Height; j++) if (data.Matrix[i, j]) simGrid[px + i, py + j] = true;
        int linesCleared = 0;
        for (int y = 0; y < h; y++) { bool full = true; for (int x = 0; x < w; x++) if (!simGrid[x, y]) { full = false; break; } if (full) { bool wasFull = true; for (int x = 0; x < w; x++) if (!originalCells[x, y]) { wasFull = false; break; } if (!wasFull) linesCleared++; } }
        for (int x = 0; x < w; x++) { bool full = true; for (int y = 0; y < h; y++) if (!simGrid[x, y]) { full = false; break; } if (full) { bool wasFull = true; for (int y = 0; y < h; y++) if (!originalCells[x, y]) { wasFull = false; break; } if (!wasFull) linesCleared++; } }
        return linesCleared;
    }

    private bool IsBoardEmptyAfterPlace(bool[,] originalCells, int w, int h, BlockData data, int px, int py)
    {
        bool[,] simGrid = (bool[,])originalCells.Clone();
        for (int i = 0; i < data.Width; i++) for (int j = 0; j < data.Height; j++) if (data.Matrix[i, j]) simGrid[px + i, py + j] = true;
        List<int> r=new List<int>(), c=new List<int>(); 
        for(int y=0;y<h;y++){bool f=true;for(int x=0;x<w;x++)if(!simGrid[x,y]){f=false;break;}if(f)r.Add(y);} 
        for(int x=0;x<w;x++){bool f=true;for(int y=0;y<h;y++)if(!simGrid[x,y]){f=false;break;}if(f)c.Add(x);}
        foreach(int y in r)for(int x=0;x<w;x++)simGrid[x,y]=false; foreach(int x in c)for(int y=0;y<h;y++)simGrid[x,y]=false;
        for(int x=0;x<w;x++)for(int y=0;y<h;y++)if(simGrid[x,y])return false; 
        return true;
    }

    // BU FONKSİYONU BlockSpawner.cs İÇİNDEKİ MEVCUT CanPlace İLE DEĞİŞTİR
    private bool CanPlace(bool[,] cells, int w, int h, BlockData data, int px, int py) 
    { 
        // 1. Sınır Kontrolü
        if (px + data.Width > w || py + data.Height > h) return false; 

        // 2. Çarpışma Kontrolü (DEBUG LOGLU)
        for (int i = 0; i < data.Width; i++) 
        {
            for (int j = 0; j < data.Height; j++) 
            {
                // Parçanın bu hücresi dolu mu?
                bool isShapePart = data.Matrix[i, j];
                
                // Grid'in o hücresi dolu mu?
                bool isGridOccupied = cells[px + i, py + j];

                // Çarpışma var mı?
                if (isShapePart && isGridOccupied) 
                {
                    // BURAYA GİRERSE SIĞMIYOR DEMEKTİR.
                    Debug.Log($"[ŞÜPHELİ] Grid dolu ({px+i},{py+j}) ama Parça ({i},{j}) boş olduğu için sığdı sayıldı!");
                    return false; 
                }
                
                // --- HATA AYIKLAMA LOGU (Sadece şüpheli durumlarda aç) ---
                // Eğer Grid doluysa (Kırmızı Küp) AMA Parça orayı boş sanıyorsa:
                if (isGridOccupied && !isShapePart)
                {
                    // Bu log sürekli akarsa, shape datası bozuk demektir.
                    // Debug.Log($"[ŞÜPHELİ] Grid dolu ({px+i},{py+j}) ama Parça ({i},{j}) boş olduğu için sığdı sayıldı!");
                }
            }
        }
        return true; 
    }
    
    private float CalculateFitScore(bool[,] cells, int w, int h, BlockData data, int px, int py) { float score = 0; int perimeter = 0; for (int lx = 0; lx < data.Width; lx++) { for (int ly = 0; ly < data.Height; ly++) { if (!data.Matrix[lx, ly]) continue; if (lx - 1 < 0 || !data.Matrix[lx - 1, ly]) perimeter++; if (lx + 1 >= data.Width || !data.Matrix[lx + 1, ly]) perimeter++; if (ly - 1 < 0 || !data.Matrix[lx, ly - 1]) perimeter++; if (ly + 1 >= data.Height || !data.Matrix[lx, ly + 1]) perimeter++; Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }; foreach(var d in dirs) { int nx = px + lx + d.x; int ny = py + ly + d.y; if (nx < 0 || nx >= w || ny < 0 || ny >= h || cells[nx, ny]) score += 1.0f; } } } return perimeter == 0 ? 0 : score / perimeter; }

    public void OnLinesCleared(int linesCount) { _targetWarmUpMoves += linesCount * rewardMovesPerLine; }
    
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
    
    // Bu fonksiyonu BlockSpawner class'ının içine ekle
    private void DebugShapeData()
    {
        Debug.Log("<color=orange>--- ŞEKİL VERİSİ KONTROLÜ ---</color>");
        foreach (var shape in allShapes)
        {
            // Ham veriyi al
            BlockData data = new BlockData(shape.ToMatrix().Trim());
        
            string matrixLog = $"<b>{shape.name}</b> (Kodun Gördüğü: {data.Width}x{data.Height}):\n";
        
            int doluSayisi = 0;
        
            // Matrisi çiz (Ters Y ekseni ile)
            for (int y = data.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    if (data.Matrix[x, y])
                    {
                        matrixLog += "<color=green>[1]</color> "; // Dolu
                        doluSayisi++;
                    }
                    else
                    {
                        matrixLog += "<color=red>[0]</color> "; // Boş
                    }
                }
                matrixLog += "\n";
            }
        
            // Eğer kodun gördüğü matris boşsa, Kırmızı Alarm ver
            if (doluSayisi == 0)
            {
                Debug.LogError($"<color=red>HATA! {shape.name} BOMBOŞ GÖRÜNÜYOR!</color>\nSen Inspector'da işaretlesen bile kod bunu okuyamıyor.");
            }
            else
            {
                Debug.Log(matrixLog);
            }
        }
        Debug.Log("<color=orange>--------------------------------</color>");
    }
}