using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Puzzle/Level Pattern")]
public class LevelPatternSO : ScriptableObject
{
    public int width = 8;
    public int height = 8;
    
    // ESKİ: public BlockColorType colorType; <-- BU GİTTİ
    
    // YENİ: Hücre verileri (Dolu mu?)
    [HideInInspector] public bool[] cells;
    
    // YENİ: Renk verileri (Hangi renk?)
    [HideInInspector] public BlockColorType[] cellColors;

    // Verileri başlatma / Yeniden Boyutlandırma
    public void ValidateArrays()
    {
        int size = width * height;
        if (cells == null || cells.Length != size) 
            cells = new bool[size];
            
        if (cellColors == null || cellColors.Length != size) 
            cellColors = new BlockColorType[size];
    }

    public bool Get(int x, int y)
    {
        if (!IsValid(x, y)) return false;
        return cells[y * width + x];
    }
    
    // O hücrenin rengini döner
    public BlockColorType GetColor(int x, int y)
    {
        if (!IsValid(x, y)) return BlockColorType.Red; // Varsayılan
        return cellColors[y * width + x];
    }
    
    public void Set(int x, int y, bool active, BlockColorType color)
    {
        if (!IsValid(x, y)) return;
        int index = y * width + x;
        cells[index] = active;
        if (active) cellColors[index] = color;
    }
    
    public void ClearCell(int x, int y)
    {
        if (!IsValid(x, y)) return;
        cells[y * width + x] = false;
    }

    private bool IsValid(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
}