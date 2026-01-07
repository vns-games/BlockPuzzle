// MatrixExtensions.cs
using System.Collections.Generic;
using UnityEngine;

public static class MatrixExtensions
{
    // SO'yu Matrise çevirir
    public static bool[,] ToMatrix(this BlockShapeSO so)
    {
        bool[,] matrix = new bool[so.width, so.height];
        for (int x = 0; x < so.width; x++)
            for (int y = 0; y < so.height; y++)
                matrix[x, y] = so.Get(x, y);
        return matrix;
    }

    // Sağa Döndürme
    public static bool[,] RotateRight(this bool[,] original)
    {
        int w = original.GetLength(0);
        int h = original.GetLength(1);
        bool[,] rotated = new bool[h, w];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                rotated[y, w - 1 - x] = original[x, y];
        
        return rotated;
    }

    // Trim (Gereksiz boşlukları atma)
    public static bool[,] Trim(this bool[,] original)
    {
        int w = original.GetLength(0);
        int h = original.GetLength(1);
        int minX = w, minY = h, maxX = 0, maxY = 0;
        bool hasBlock = false;

        for(int x=0; x<w; x++)
            for(int y=0; y<h; y++)
                if (original[x, y])
                {
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                    hasBlock = true;
                }

        if (!hasBlock) return original;

        int newW = maxX - minX + 1;
        int newH = maxY - minY + 1;
        bool[,] trimmed = new bool[newW, newH];

        for(int x=0; x<newW; x++)
            for(int y=0; y<newH; y++)
                trimmed[x, y] = original[minX + x, minY + y];

        return trimmed;
    }
    // --- SATIR VE SÜTUN KONTROLLERİ ---

    // Dolu satırların indekslerini döndürür
    public static List<int> GetFullRows(this bool[,] grid)
    {
        List<int> fullRows = new List<int>();
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            bool isFull = true;
            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y]) // Bir tane bile boş varsa
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) fullRows.Add(y);
        }
        return fullRows;
    }

    // Dolu sütunların indekslerini döndürür
    public static List<int> GetFullColumns(this bool[,] grid)
    {
        List<int> fullCols = new List<int>();
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            bool isFull = true;
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y])
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) fullCols.Add(x);
        }
        return fullCols;
    }
}