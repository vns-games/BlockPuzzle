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

        // A) Mantıksal Veriyi Sil
        if (!grid.Cells[x, y]) return; // Zaten boşsa işlem yapma (Opsiyonel optimizasyon)
        grid.Cells[x, y] = false;

        // B) Görseli Havuza Gönder
        if (grid.Visuals[x, y] != null)
        {
            if (CellVisualPool.Instance != null)
            {
                CellVisualPool.Instance.Release(grid.Visuals[x, y]);
            }
            else
            {
                GameObject.Destroy(grid.Visuals[x, y]); // Pool yoksa yok et (Güvenlik)
            }
            grid.Visuals[x, y] = null;
        }
    }

    // Satır ve Sütunları tarar, temizler ve temizlenen sayıyı döner
    public static int CheckAndClearMatches(this Grid grid)
    {
        List<int> rows = new List<int>();
        List<int> cols = new List<int>();

        // 1. Dolu Satırları Bul
        for (int y = 0; y < grid.Height; y++)
        {
            bool full = true;
            for (int x = 0; x < grid.Width; x++) if (!grid.Cells[x, y]) { full = false; break; }
            if (full) rows.Add(y);
        }

        // 2. Dolu Sütunları Bul
        for (int x = 0; x < grid.Width; x++)
        {
            bool full = true;
            for (int y = 0; y < grid.Height; y++) if (!grid.Cells[x, y]) { full = false; break; }
            if (full) cols.Add(x);
        }

        // 3. Temizle
        int totalCleared = rows.Count + cols.Count;
        if (totalCleared > 0)
        {
            foreach (var r in rows) grid.ClearRow(r);
            foreach (var c in cols) grid.ClearColumn(c);
        }

        return totalCleared;
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