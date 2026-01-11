using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Puzzle/Level Pattern")]
public class LevelPatternSO : ScriptableObject
{
    public int width = 8;
    public int height = 8;
    
    [Tooltip("Bu desendeki blokların rengi")]
    public Color patternColor = Color.white;
    
    [HideInInspector] 
    public bool[] cells = new bool[64]; 

    public bool Get(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return cells[y * width + x];
    }
    
    public void Set(int x, int y, bool val)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            cells[y * width + x] = val;
    }
}