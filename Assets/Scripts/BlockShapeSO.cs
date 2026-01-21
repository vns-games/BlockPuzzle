using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Block Shape")]
public class BlockShapeSO : ScriptableObject
{
    public int width = 3;
    public int height = 3;
    public bool[] cells; 

    // --- MEVCUT KODLARIN BURADA KALSIN (Resize, Get, Set vs.) ---
    public void Resize()
    {
        if (cells == null || cells.Length != width * height)
            cells = new bool[width * height];
    }
    public bool Get(int x, int y) => cells[y * width + x];
    public void Set(int x, int y, bool value) => cells[y * width + x] = value;
    
    public bool[,] ToMatrix()
    {
        bool[,] matrix = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                matrix[x, y] = cells[y * width + x];
        return matrix;
    }

    // --- YENİ EKLENECEK GÜVENLİ FONKSİYON ---
    public bool[,] GetTrimmedMatrix()
    {
        // 1. Matrisi al
        bool[,] original = ToMatrix();
        int w = original.GetLength(0);
        int h = original.GetLength(1);

        // 2. Sınırları Bul
        int minX = w, maxX = 0;
        int minY = h, maxY = 0;
        bool hasData = false;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (original[x, y])
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    hasData = true;
                }
            }
        }

        // Eğer şekil boşsa 1x1'lik boş bir matris dön
        if (!hasData) return new bool[1, 1];

        // 3. Yeni, Kırpılmış Matrisi Oluştur
        int newW = maxX - minX + 1;
        int newH = maxY - minY + 1;
        bool[,] trimmed = new bool[newW, newH];

        // 4. Veriyi Kopyala
        for (int x = 0; x < newW; x++)
        {
            for (int y = 0; y < newH; y++)
            {
                trimmed[x, y] = original[minX + x, minY + y];
            }
        }

        return trimmed;
    }
}