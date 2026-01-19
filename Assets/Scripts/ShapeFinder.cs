using System.Collections.Generic;
using UnityEngine;

public static class ShapeFinder 
{
    // ==================================================================================
    // 1. TEMEL FONKSİYONLAR (FITS)
    // ==================================================================================

    /// <summary>
    /// Verilen listeden sadece gride sığabilenleri döndürür.
    /// </summary>
    public static List<BlockShapeSO> GetFits(Grid grid, List<BlockShapeSO> candidates)
    {
        var fits = new List<BlockShapeSO>();
        foreach (var shape in candidates)
        {
            if (grid.CanFitAnywhere(new BlockData(shape.ToMatrix().Trim())))
            {
                fits.Add(shape);
            }
        }
        return fits;
    }

    // ==================================================================================
    // 2. WARMUP & STRATEJİ FONKSİYONLARI
    // ==================================================================================

    /// <summary>
    /// [1. ÖNCELİK] MEGA KILL
    /// Bir yere konduğunda 3 veya daha fazla satır/sütun silen parçaları bulur.
    /// </summary>
    public static List<BlockShapeSO> GetMegaKillers(Grid grid, List<BlockShapeSO> candidates)
    {
        var list = new List<BlockShapeSO>();
        
        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            
            // Eğer parça çok küçükse Mega Kill yapması imkansızdır (Optimizasyon)
            if (data.Width < 3 && data.Height < 3) continue;

            int maxClear = GetMaxPotentialClear(grid, data);
            
            // 3 veya daha fazla satır siliyorsa listeye ekle
            if (maxClear >= 3) list.Add(shape);
        }
        return list;
    }

    /// <summary>
    /// [2. ÖNCELİK] PERFECT FITS (TETRİS HİSSİ)
    /// Büyük parçaların (Mass >= 3) mevcut bloklara temas yüzeyi yüksekse (Cuk oturuyorsa) seçer.
    /// </summary>
    public static List<BlockShapeSO> GetLargePerfectFits(Grid grid, List<BlockShapeSO> candidates, float threshold)
    {
        var list = new List<BlockShapeSO>();
        var solidFits = new List<BlockShapeSO>(); // Tam dolu olanlar (Kare, Çubuk)

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());

            // 1. KÜTLE KONTROLÜ (Çok küçük parçaları ele)
            int mass = CountMass(data);
            if (mass < 3) continue;

            // 2. TEMAS KONTROLÜ
            float bestRatio = GetBestContactRatio(grid, data);
            
            if (bestRatio >= threshold)
            {
                // BURASI YENİ:
                // Parça tam bir dikdörtgen mi? (Deliksiz mi?)
                // Örn: 3x3 Kare -> Alan 9, Kütle 9 -> TAM DOLU
                // Örn: U Şekli -> Alan 9, Kütle 7 -> EKSİK
                int area = data.Width * data.Height;
                bool isSolid = (mass == area);

                if (isSolid)
                {
                    solidFits.Add(shape); // Öncelikli listeye ekle
                }
                else
                {
                    list.Add(shape); // Normal listeye ekle
                }
            }
        }

        // Eğer tam dolu (Solid) parça bulduysak SADECE onları döndür.
        // Böylece U şekli yerine Kare şekli gelir.
        if (solidFits.Count > 0) return solidFits;

        // Yoksa diğerlerini döndür
        return list;
    }

    /// <summary>
    /// [3. ÖNCELİK] CLEAN KILLERS
    /// Ortalığı dağıtmadan (veya çok az boşluk bırakarak) 1 veya 2 satır silen parçalar.
    /// </summary>
    public static List<BlockShapeSO> GetCleanKillers(Grid grid, List<BlockShapeSO> candidates)
    {
        var list = new List<BlockShapeSO>();

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            int maxClear = GetMaxPotentialClear(grid, data);

            // 1 veya 2 satır silsin (Mega Kill değil, basit temizlik)
            if (maxClear > 0 && maxClear < 3)
            {
                list.Add(shape);
            }
        }
        return list;
    }

    /// <summary>
    /// [4. ÖNCELİK / SMART HELP] HOLE FILLERS
    /// Boşluklara yüksek oranda temas eden (Sıkışan) parçalar. 
    /// Small parçalar dahildir.
    /// </summary>
    public static List<BlockShapeSO> GetHoleFillers(Grid grid, List<BlockShapeSO> candidates, float threshold)
    {
        var list = new List<BlockShapeSO>();

        foreach (var shape in candidates)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            float bestRatio = GetBestContactRatio(grid, data);

            if (bestRatio >= threshold) list.Add(shape);
        }
        return list;
    }

    // ==================================================================================
    // 3. HESAPLAMA VE SİMÜLASYON MOTORU (CORE)
    // ==================================================================================

    /// <summary>
    /// Bir parçanın grid üzerindeki herhangi bir pozisyonda silebileceği MAKSİMUM satır sayısını döner.
    /// </summary>
    private static int GetMaxPotentialClear(Grid grid, BlockData data)
    {
        int maxLines = 0;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.CanPlace(data, x, y))
                {
                    int lines = SimulateClearCount(grid, data, x, y);
                    if (lines > maxLines) maxLines = lines;
                    
                    // Ufak bir optimizasyon: Mega Kill bulduysak daha fazla aramaya gerek yok (isteğe bağlı)
                    // if (maxLines >= 3) return maxLines; 
                }
            }
        }
        return maxLines;
    }

    /// <summary>
    /// Bir parçanın grid üzerindeki en iyi "Temas Oranını" (Komşuluk) döner.
    /// 0.0 (Havada) ile 1.0 (Tamamen gömülü) arasındadır.
    /// </summary>
    private static float GetBestContactRatio(Grid grid, BlockData data)
    {
        float maxRatio = 0f;
        int piecePerimeter = CalculatePerimeter(data); // Parçanın çevresi

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (grid.CanPlace(data, x, y))
                {
                    int contactPoints = CountContacts(grid, data, x, y);
                    float ratio = (float)contactPoints / (float)piecePerimeter;
                    
                    // Grid kenarlarına değmeyi de temas sayalım (Köşeleri sevmesi için)
                    // Amaç "cuk oturmak"
                    if (ratio > maxRatio) maxRatio = ratio;
                }
            }
        }
        return maxRatio;
    }

    // --- YARDIMCI: SİLİNECEK SATIR SİMÜLASYONU ---
    private static int SimulateClearCount(Grid grid, BlockData data, int px, int py)
    {
        int totalCleared = 0;

        // Yatay Satırlar (Rows)
        for (int y = 0; y < grid.Height; y++)
        {
            // Sadece parçanın etkilediği Y aralığına bakmak yeterli (Optimizasyon)
            bool isRowAffected = (y >= py && y < py + data.Height);
            
            bool full = true;
            for (int x = 0; x < grid.Width; x++)
            {
                bool cellFull = grid.Cells[x, y];
                
                // Eğer parça buraya gelecekse dolu say
                if (!cellFull && isRowAffected)
                {
                    int localX = x - px;
                    int localY = y - py;
                    if (localX >= 0 && localX < data.Width) // Y kontrolü zaten üstte
                        if (data.Matrix[localX, localY]) cellFull = true;
                }

                if (!cellFull) { full = false; break; }
            }
            if (full) totalCleared++;
        }

        // Dikey Sütunlar (Cols)
        for (int x = 0; x < grid.Width; x++)
        {
            bool isColAffected = (x >= px && x < px + data.Width);

            bool full = true;
            for (int y = 0; y < grid.Height; y++)
            {
                bool cellFull = grid.Cells[x, y];
                
                if (!cellFull && isColAffected)
                {
                    int localX = x - px;
                    int localY = y - py;
                    if (localY >= 0 && localY < data.Height)
                        if (data.Matrix[localX, localY]) cellFull = true;
                }

                if (!cellFull) { full = false; break; }
            }
            if (full) totalCleared++;
        }

        return totalCleared;
    }

    // --- YARDIMCI: TEMAS SAYAR ---
    private static int CountContacts(Grid grid, BlockData data, int px, int py)
    {
        int contacts = 0;
        
        // Parçanın her dolu hücresi için 4 yanına bak
        for (int lx = 0; lx < data.Width; lx++)
        {
            for (int ly = 0; ly < data.Height; ly++)
            {
                if (!data.Matrix[lx, ly]) continue;

                int worldX = px + lx;
                int worldY = py + ly;

                // Komşu koordinatlar (Sağ, Sol, Üst, Alt)
                Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

                foreach (var dir in dirs)
                {
                    int nx = worldX + dir.x;
                    int ny = worldY + dir.y;

                    // Grid dışı (Duvarlar) temas sayılır mı? Evet, "Köşe" hissi için.
                    if (!grid.IsInside(nx, ny)) 
                    {
                        contacts++;
                    }
                    // İçerdeyse ve doluysa temas var
                    else if (grid.Cells[nx, ny])
                    {
                        contacts++;
                    }
                }
            }
        }
        return contacts;
    }

    // --- MATEMATİK YARDIMCILARI ---
    private static int CountMass(BlockData data)
    {
        int c = 0;
        foreach (bool b in data.Matrix) if (b) c++;
        return c;
    }

    private static int CalculatePerimeter(BlockData data)
    {
        // Basitçe: Her dolu karenin 4 kenarı vardır.
        // Komşusu olan kenarlar kapanır.
        // Formül: (Dolu Hücre * 4) - (İç Temaslar * 2)
        int mass = CountMass(data);
        int internalAdjacency = 0;

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                
                // Sadece Sağa ve Yukarı bakmak (çift saymamak için) yeterli
                if (x + 1 < data.Width && data.Matrix[x + 1, y]) internalAdjacency++;
                if (y + 1 < data.Height && data.Matrix[x, y + 1]) internalAdjacency++;
            }
        }
        return (mass * 4) - (internalAdjacency * 2);
    }
    
    public static BlockShapeSO FindPotentialMegaKiller(Grid grid, List<BlockShapeSO> allShapes)
    {
        // Tüm şekilleri tara
        foreach (var shape in allShapes)
        {
            BlockData data = new BlockData(shape.ToMatrix().Trim());
            
            // Çok küçük parçalarla zaten Mega Kill olmaz, performans için atla
            if (data.Width < 2 && data.Height < 2) continue;

            // Bu parça tahtada bir yere konduğunda 3 veya daha fazla satır siliyor mu?
            int potentialClear = GetMaxPotentialClear(grid, data);

            if (potentialClear >= 3)
            {
                // Evet! Bu parça bir kahraman.
                return shape;
            }
        }
        return null;
    }
    
    /// <summary>
    /// [WARMUP ÖZEL]
    /// Sadece gride sığanları bulur AMA küçük parçaları (1x1, 2x1) eler.
    /// Eğer sığacak büyük parça yoksa, mecburen küçükleri verir (Oyun tıkanmasın diye).
    /// </summary>
    public static List<BlockShapeSO> GetSatisfyingFits(Grid grid, List<BlockShapeSO> candidates)
    {
        var allFits = new List<BlockShapeSO>();
        var meatyFits = new List<BlockShapeSO>(); // "Etli/Dolgun" parçalar (Mass >= 3)

        foreach (var shape in candidates)
        {
            var rawMatrix = shape.ToMatrix().Trim();
            BlockData data = new BlockData(rawMatrix);

            // Önce sığıyor mu diye bak?
            if (grid.CanFitAnywhere(data))
            {
                allFits.Add(shape);

                // Şimdi "Tatmin Edici" mi diye bak (Hücre sayısı 3 veya daha fazla mı?)
                // 1x1 (Mass 1) ve 2x1 (Mass 2) buraya giremez.
                if (CountMass(data) >= 3)
                {
                    meatyFits.Add(shape);
                }
            }
        }

        // KURAL: Eğer elimizde sığan "Büyük" parçalar varsa, sadece onları döndür.
        // Böylece oyuncu 1x1 gibi gıcık parçaları görmez.
        if (meatyFits.Count > 0)
        {
            return meatyFits;
        }

        // Eğer büyük parça sığmıyorsa (alan çok darsa), mecburen ne varsa onu döndür.
        return allFits;
    }
}