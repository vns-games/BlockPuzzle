using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Block Shape")]
public class BlockShapeSO : ScriptableObject
{
    public int width = 3;
    public int height = 3;
    public bool[] cells; 

    // --- KRİTİK DÜZELTME ---
    // [System.NonSerialized] etiketi, Unity'nin bu değişkeni kaydetmeye çalışmasını engeller.
    // Eğer Unity bunu kaydederse, bool[,] desteklenmediği için Matrix null olur ve oyun çöker.
    [System.NonSerialized]
    private BlockData _cachedData;

    // "Data" bir değişken değil, ÖZELLİK (Property) olmalı.
    public BlockData Data 
    {
        get
        {
            // Eğer veri yoksa veya oyun yeniden başladıysa (null ise) oluştur.
            if (_cachedData == null)
            {
                var matrix = ToMatrix().Trim();
                _cachedData = new BlockData(matrix);
            }
            return _cachedData;
        }
    }

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
}