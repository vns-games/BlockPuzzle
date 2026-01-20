using System.Collections.Generic;
using UnityEngine;

public static class ShapeFinder 
{
    // ==================================================================================
    // 1. FİLTRELEME METOTLARI
    // ==================================================================================
    
    public static List<BlockShapeSO> GetFits(Grid grid, List<BlockShapeSO> candidates)
    {
        var fits = new List<BlockShapeSO>();
        foreach (var shape in candidates)
        {
            if (grid.CanFitAnywhere(new BlockData(shape.ToMatrix().Trim())))
                fits.Add(shape);
        }
        return fits;
    }

    public static List<BlockShapeSO> GetBigFits(Grid grid, List<BlockShapeSO> candidates, int minSize)
    {
        var bigFits = new List<BlockShapeSO>();
        foreach (var shape in candidates)
        {
            var rawMatrix = shape.ToMatrix().Trim();
            BlockData data = new BlockData(rawMatrix);
            if ((data.Width * data.Height) >= minSize)
            {
                if (grid.CanFitAnywhere(data)) bigFits.Add(shape);
            }
        }
        return bigFits.Count > 0 ? bigFits : GetFits(grid, candidates);
    }

    public static List<BlockShapeSO> GetSatisfyingFits(Grid grid, List<BlockShapeSO> candidates)
    {
        var allFits = new List<BlockShapeSO>();
        var meatyFits = new List<BlockShapeSO>(); 
        foreach (var shape in candidates)
        {
            var rawMatrix = shape.ToMatrix().Trim();
            BlockData data = new BlockData(rawMatrix);
            if (grid.CanFitAnywhere(data))
            {
                allFits.Add(shape);
                if (CountMass(data) >= 3) meatyFits.Add(shape);
            }
        }
        return meatyFits.Count > 0 ? meatyFits : allFits;
    }

    // ==================================================================================
    // 2. STRATEJİ: PERFECT FIT (SAF PUANLAMA - BONUSSUZ)
    // ==================================================================================

    public static List<BlockShapeSO> GetLargePerfectFits(Grid grid, List<BlockShapeSO> candidates, float threshold)
    {
        var scoredCandidates = new List<KeyValuePair<BlockShapeSO, float>>();

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());

            // 1x1 gibi çok küçükleri ele (Tetris hissi vermezler)
            if (CountMass(data) < 2) continue;

            int perimeter = CalculateExternalPerimeter(data);
            if (perimeter == 0) continue;

            // 1. Temas Puanı (Duvarlar ve Bloklar Eşit: 1.0 Puan)
            float weightedContacts = GetMaxWeightedScore(grid, data);
            
            // 2. Soket (Yuva) Bonusu
            // Bu hala gerekli çünkü bir deliğe girmek, yana yaslanmaktan daha değerlidir.
            int socketCount = GetMaxSocketScore(grid, data);
            float socketBonus = socketCount * 3.0f; 

            // FORMÜL: Sadece Temas ve Soket. Torpil yok.
            float totalScore = (weightedContacts + socketBonus) / (float)perimeter;

            // --- İPTAL EDİLDİ: Complex Shape Bonusu ---
            // if (IsComplexShape(data)) totalScore += 0.20f; 
            // ------------------------------------------

            if (totalScore >= threshold)
            {
                scoredCandidates.Add(new KeyValuePair<BlockShapeSO, float>(shape, totalScore));
            }
        }

        // Puana göre sırala
        scoredCandidates.Sort((a, b) => b.Value.CompareTo(a.Value));

        List<BlockShapeSO> result = new List<BlockShapeSO>();
        foreach (var kvp in scoredCandidates) result.Add(kvp.Key);

        return result;
    }

    // ==================================================================================
    // 3. KILLERS & HELPERS
    // ==================================================================================

    // ==================================================================================
    // 3. KILLERS & HELPERS (GÜNCELLENDİ: DEBUG MODU)
    // ==================================================================================

    public static List<BlockShapeSO> GetMegaKillers(Grid grid, List<BlockShapeSO> candidates)
    {
        var list = new List<BlockShapeSO>();
        
        // Debug için en iyi skoru tutalım
        int globalMax = 0;
        string bestShapeName = "None";

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            
            // --- DEĞİŞİKLİK 1: Optimizasyonu kaldırdık. Küçük-büyük her şeye baksın. ---
            // if (data.Width < 3 && data.Height < 3) continue; 

            int maxClear = GetMaxPotentialClear(grid, data);
            
            // --- DEBUG LOG ---
            // Eğer 2 veya daha fazla siliyorsa konsola yaz (Görelim)
            if(maxClear >= 2) 
            {
                Debug.Log($"[MEGA-CHECK] Parça: {shape.name} | Sildiği Satır: {maxClear}");
            }

            if (maxClear >= 3) 
            {
                list.Add(shape);
            }

            if(maxClear > globalMax) { globalMax = maxClear; bestShapeName = shape.name; }
        }

        if(list.Count == 0 && globalMax > 0)
        {
            Debug.LogWarning($"[MEGA-FAIL] Hiçbir parça 3 satır silemedi. En iyi: {bestShapeName} ({globalMax} satır)");
        }

        return list;
    }

    public static List<BlockShapeSO> GetCleanKillers(Grid grid, List<BlockShapeSO> candidates)
    {
        var list = new List<BlockShapeSO>();
        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            int clear = GetMaxPotentialClear(grid, data);
            if (clear > 0 && clear < 3) list.Add(shape);
        }
        return list;
    }

    public static List<BlockShapeSO> GetHoleFillers(Grid grid, List<BlockShapeSO> candidates, float threshold)
    {
        var list = new List<BlockShapeSO>();
        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            int perimeter = CalculateExternalPerimeter(data);
            if (perimeter == 0) continue;
            int maxC = GetMaxContactCount(grid, data); 
            if ((float)maxC / perimeter >= threshold) list.Add(shape);
        }
        return list;
    }

    // ==================================================================================
    // 4. HESAPLAMA MOTORU
    // ==================================================================================

    private static float GetMaxWeightedScore(Grid grid, BlockData data)
    {
        float maxScore = 0f;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.CanPlace(data, x, y))
                {
                    float currentScore = CalculateWeightedContacts(grid, data, x, y);
                    if (currentScore > maxScore) maxScore = currentScore;
                }
            }
        }
        return maxScore;
    }

    private static float CalculateWeightedContacts(Grid grid, BlockData data, int px, int py)
    {
        float score = 0f;
        
        // EŞİT PUANLAMA (Senin son isteğin)
        float blockReward = 1.0f;
        float wallReward = 1.0f; 

        // --- DEBUG AYARLARI ---
        Vector3 gridOrigin = GridManager.Instance.transform.position;
        float cellSize = 0.5f; 
        Vector3 halfCell = new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0); 

        for (int lx = 0; lx < data.Width; lx++)
        {
            for (int ly = 0; ly < data.Height; ly++)
            {
                if (!data.Matrix[lx, ly]) continue;

                int gridX = px + lx;
                int gridY = py + ly;
                Vector3 startPos = gridOrigin + new Vector3(gridX * cellSize, gridY * cellSize, 0) + halfCell;

                // 8 YÖN
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; 
                        
                        int nx = gridX + dx;
                        int ny = gridY + dy;
                        Vector3 endPos = gridOrigin + new Vector3(nx * cellSize, ny * cellSize, 0) + halfCell;

                        if (!grid.IsInside(nx, ny)) 
                        {
                            score += wallReward;
                            Debug.DrawLine(startPos, endPos, Color.red, 4);
                        }
                        else if (grid.Cells[nx, ny])
                        {
                            score += blockReward;
                            Debug.DrawLine(startPos, endPos, Color.green, 4);
                        }
                    }
                }
            }
        }
        return score;
    }

    private static int GetMaxSocketScore(Grid grid, BlockData data)
    {
        int maxSockets = 0; for (int x = 0; x < grid.Width; x++) for (int y = 0; y < grid.Height; y++) 
        if (grid.CanPlace(data, x, y)) { int current = CountSockets(grid, data, x, y); if (current > maxSockets) maxSockets = current; } return maxSockets;
    }
    private static int CountSockets(Grid grid, BlockData data, int px, int py)
    {
        int socketCount = 0; for (int lx = 0; lx < data.Width; lx++) for (int ly = 0; ly < data.Height; ly++) 
        if (data.Matrix[lx, ly]) { int obstacles = 0; int wx = px + lx; int wy = py + ly;
        if (!grid.IsInside(wx + 1, wy) || grid.Cells[wx + 1, wy]) obstacles++;
        if (!grid.IsInside(wx - 1, wy) || grid.Cells[wx - 1, wy]) obstacles++;
        if (!grid.IsInside(wx, wy + 1) || grid.Cells[wx, wy + 1]) obstacles++;
        if (!grid.IsInside(wx, wy - 1) || grid.Cells[wx, wy - 1]) obstacles++;
        if (obstacles >= 3) socketCount++; } return socketCount;
    }
    private static int GetMaxContactCount(Grid grid, BlockData data)
    {
        int maxContacts = 0; for (int x = 0; x < grid.Width; x++) for (int y = 0; y < grid.Height; y++) 
        if (grid.CanPlace(data, x, y)) { int current = CountContacts(grid, data, x, y); if (current > maxContacts) maxContacts = current; } return maxContacts;
    }
    private static int CountContacts(Grid grid, BlockData data, int px, int py)
    {
        int contacts = 0; for (int lx = 0; lx < data.Width; lx++) for (int ly = 0; ly < data.Height; ly++) 
        if (data.Matrix[lx, ly]) { Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (var dir in dirs) { int nx = px + lx + dir.x; int ny = py + ly + dir.y; if (!grid.IsInside(nx, ny) || grid.Cells[nx, ny]) contacts++; } } return contacts;
    }
    private static int CalculateExternalPerimeter(BlockData data)
    {
        int perimeter = 0; for (int x = 0; x < data.Width; x++) for (int y = 0; y < data.Height; y++) 
        if (data.Matrix[x, y]) { if (x - 1 < 0 || !data.Matrix[x - 1, y]) perimeter++; if (x + 1 >= data.Width || !data.Matrix[x + 1, y]) perimeter++; if (y - 1 < 0 || !data.Matrix[x, y - 1]) perimeter++; if (y + 1 >= data.Height || !data.Matrix[x, y + 1]) perimeter++; } return perimeter;
    }
    private static int CountMass(BlockData data) { int c = 0; foreach (bool b in data.Matrix) if (b) c++; return c; }
    private static int GetMaxPotentialClear(Grid grid, BlockData data)
    {
        int maxLines = 0; for (int x = 0; x < grid.Width; x++) for (int y = 0; y < grid.Height; y++) 
        if (grid.CanPlace(data, x, y)) { int lines = SimulateClearCount(grid, data, x, y); if (lines > maxLines) maxLines = lines; } return maxLines;
    }
    private static int SimulateClearCount(Grid grid, BlockData data, int px, int py)
    {
        int totalCleared = 0; for (int y = 0; y < grid.Height; y++) { bool isRowAffected = (y >= py && y < py + data.Height); bool full = true; for (int x = 0; x < grid.Width; x++) { bool cellFull = grid.Cells[x, y]; if (!cellFull && isRowAffected) { int localX = x - px; int localY = y - py; if (localX >= 0 && localX < data.Width) if (data.Matrix[localX, localY]) cellFull = true; } if (!cellFull) { full = false; break; } } if (full) totalCleared++; }
        for (int x = 0; x < grid.Width; x++) { bool isColAffected = (x >= px && x < px + data.Width); bool full = true; for (int y = 0; y < grid.Height; y++) { bool cellFull = grid.Cells[x, y]; if (!cellFull && isColAffected) { int localX = x - px; int localY = y - py; if (localY >= 0 && localY < data.Height) if (data.Matrix[localX, localY]) cellFull = true; } if (!cellFull) { full = false; break; } } if (full) totalCleared++; } return totalCleared;
    }
}