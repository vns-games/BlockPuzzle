using UnityEngine;
[CreateAssetMenu(menuName = "Puzzle/Grid Settings")]
public class GridSettingsSO : ScriptableObject
{
    public int rows = 8;
    public int columns = 8;
    public float cellSize = 1f;
    public float gap = 0.1f;
    public Color emptyColor = Color.gray;
    public Color filledColor = Color.cyan;
}