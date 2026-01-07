using UnityEngine;
[System.Serializable]
public class BlockShape
{
    public bool[,] cells;

    public int Width => cells.GetLength(0);
    public int Height => cells.GetLength(1);
    
    public void RotateRight()
    {
        bool[,] rotated =
            new bool[Height, Width];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                rotated[y, Width - 1 - x] = cells[x, y];
            }
        }

        cells = rotated;
        Trim();
    }
    public void Trim()
    {
        int minX = Width, minY = Height;
        int maxX = 0, maxY = 0;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (cells[x, y])
                {
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }

        int w = maxX - minX + 1;
        int h = maxY - minY + 1;

        bool[,] trimmed = new bool[w, h];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                trimmed[x, y] = cells[minX + x, minY + y];

        cells = trimmed;
    }

}