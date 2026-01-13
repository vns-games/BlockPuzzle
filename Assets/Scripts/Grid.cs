using UnityEngine;

[System.Serializable]
public class Grid
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public float CellSize { get; private set; }

    // Mantıksal Matris (0 ve 1)
    public bool[,] Cells;
    
    // Görsel Matris (Ekranda görünen objeler)
    public GameObject[,] Visuals;

    public Grid(int width, int height, float cellSize)
    {
        Width = width;
        Height = height;
        CellSize = cellSize;
        Cells = new bool[width, height];
        Visuals = new GameObject[width, height];
    }

    public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
}