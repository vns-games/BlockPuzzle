using System.Collections.Generic;
using UnityEngine;

// "this Grid grid" sayesinde bu metodları sanki Grid'in kendi metoduymuş gibi çağırabilirsin.
public static class GridLogic
{
    // --- 1. YERLEŞTİRME KURALLARI ---

    public static bool CanPlace(this Grid grid, BlockData data, int startX, int startY)
    {
        if (data == null)
        {
            Debug.LogError("CanPlace fonksiyonuna NULL veri geldi! BlockShapeSO ayarlarını kontrol et.");
            return false;
        }
        
        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;
                int gx = startX + x;
                int gy = startY + y;

                if (!grid.IsInside(gx, gy)) return false; // Dışarı taştı
                if (grid.Cells[gx, gy]) return false;     // Zaten dolu
            }
        }
        return true;
    }

    public static bool CanFitAnywhere(this Grid grid, BlockData data)
    {
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                if (grid.CanPlace(data, x, y)) return true;
        return false;
    }

    // --- 2. TEMİZLİK VE GÖRSEL YÖNETİMİ (EKSİK OLAN KISIM) ---

    // Tek bir hücreyi temizler (Hem veriyi hem görseli)
    public static void ClearCell(this Grid grid, int x, int y)
    {
        if (!grid.IsInside(x, y)) return;

        // A) Mantıksal Veriyi Sil (Her durumda false yap, kontrol etme)
        grid.Cells[x, y] = false;

        // B) Görseli Havuza Gönder
        if (grid.Visuals[x, y] != null)
        {
            if (CellVisualPool.Instance != null)
            {
                // Havuza iade et
                CellVisualPool.Instance.Release(grid.Visuals[x, y]);
            }
            else
            {
                // Eğer oyun kapanıyorsa ve Pool yok olduysa mecburen Destroy
                Object.Destroy(grid.Visuals[x, y]); 
            }
        
            // Referansı kopar
            grid.Visuals[x, y] = null;
        }
    }

    // Satır ve Sütunları tarar, temizler ve temizlenen sayıyı döner
    public static int CheckAndClearMatches(this Grid grid)
    {
        List<int> rowsToClear = new List<int>();
        List<int> colsToClear = new List<int>();

        // 1. DOLU SATIRLARI TESPİT ET
        for (int y = 0; y < grid.Height; y++)
        {
            bool full = true;
            for (int x = 0; x < grid.Width; x++)
            {
                if (!grid.Cells[x, y]) 
                { 
                    full = false; 
                    break; 
                }
            }
            if (full) rowsToClear.Add(y);
        }

        // 2. DOLU SÜTUNLARI TESPİT ET
        for (int x = 0; x < grid.Width; x++)
        {
            bool full = true;
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.Cells[x, y]) 
                { 
                    full = false; 
                    break; 
                }
            }
            if (full) colsToClear.Add(x);
        }

        // Eğer patlayan yoksa 0 dön ve çık
        if (rowsToClear.Count == 0 && colsToClear.Count == 0) return 0;

        // 3. SİLİNECEK KARELERİ İŞARETLE (Duplicate önleme)
        // Kesişen satır ve sütunlardaki blokları iki kere silmeye çalışmamak için
        // önce "Silinecekler Listesi" (bool array) oluşturuyoruz.
        bool[][] blocksToRemove = new bool[grid.Width][];
        for(int index = 0; index < grid.Width; index++)
        {
            blocksToRemove[index] = new bool[grid.Height];
        }

        foreach (int y in rowsToClear)
            for (int x = 0; x < grid.Width; x++) blocksToRemove[x][y] = true;

        foreach (int x in colsToClear)
            for (int y = 0; y < grid.Height; y++) blocksToRemove[x][y] = true;

        // 4. TEMİZLİK VE SAYIM İŞLEMİ
        int uniqueBlocksCleared = 0;

        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                // Bu kare silinecekler listesinde mi?
                if (blocksToRemove[x][y])
                {
                    // Kare gerçekten doluysa işlem yap (Boş hücreyi sayma)
                    if (grid.Cells[x, y])
                    {
                        // A) Sayacı artır
                        uniqueBlocksCleared++;

                        // B) Mantıksal veriyi sil
                        grid.Cells[x, y] = false;

                        // C) Görsel veriyi sil (Havuza iade et)
                        if (grid.Visuals[x, y] != null)
                        {
                            // GameObject üzerinden VisualCell scriptini al
                            VisualCell cell = grid.Visuals[x, y].GetComponent<VisualCell>();
                            
                            // Statik havuza geri gönder
                            StaticCellPool.Despawn(cell);
                            
                            // Referansı temizle
                            grid.Visuals[x, y] = null;
                        }
                    }
                }
            }
        }

        // Toplam silinen kare sayısını dön (Skor için)
        return uniqueBlocksCleared;
    }
    // Yardımcı: Tüm satırı sil
    public static void ClearRow(this Grid grid, int y)
    {
        for (int x = 0; x < grid.Width; x++) grid.ClearCell(x, y);
    }

    // Yardımcı: Tüm sütunu sil
    public static void ClearColumn(this Grid grid, int x)
    {
        for (int y = 0; y < grid.Height; y++) grid.ClearCell(x, y);
    }

    // --- 3. İSTATİSTİK ---

    public static float GetFillPercentage(this Grid grid)
    {
        int count = 0;
        int total = grid.Width * grid.Height;
        foreach (var c in grid.Cells) if (c) count++;
        return (float)count / total;
    }
}