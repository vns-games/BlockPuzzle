using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    public Vector2 gridOrigin = new Vector2(0, 0);
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 1f;
    public Color gridColor = Color.green;

    void OnDrawGizmos()
    {
        Gizmos.color = gridColor;

        // Dikey çizgiler
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y, 0);
            Vector3 end = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y + gridHeight * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        // Yatay çizgiler
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = new Vector3(gridOrigin.x, gridOrigin.y + y * cellSize, 0);
            Vector3 end = new Vector3(gridOrigin.x + gridWidth * cellSize, gridOrigin.y + y * cellSize, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}