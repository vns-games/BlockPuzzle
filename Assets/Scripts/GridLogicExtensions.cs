public static class GridLogicExtensions
{
    // Bir koordinatın grid içinde olup olmadığını kontrol eder
    public static bool IsValidCoordinate(this bool[,] grid, int x, int y)
    {
        return x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1);
    }

    // Blok belirli bir (gx, gy) noktasına sığar mı?
    public static bool CanPlaceShape(this bool[,] grid, BlockData shape, int gx, int gy)
    {
        for (int x = 0; x < shape.Width; x++)
        {
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.Matrix[x, y]) continue; // Boş hücreleri geç

                int targetX = gx + x;
                int targetY = gy + y;

                // 1. Grid dışına taşıyor mu?
                if (!grid.IsValidCoordinate(targetX, targetY)) return false;

                // 2. Hedef hücre zaten dolu mu?
                if (grid[targetX, targetY]) return false;
            }
        }
        return true;
    }
}

// Blokların çalışma zamanındaki halini tutar.