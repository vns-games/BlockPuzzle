using UnityEngine;
public class GridVisual : MonoBehaviour
{
    // GridManager'dan sadece veri alır, ne yapacağına karar vermez.
    public void PlaceCellVisual(GameObject cellPrefab, int x, int y, float cellSize, Transform parent)
    {
        GameObject visual = Instantiate(cellPrefab, parent);
        visual.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0);
        // İsimlendirme debug için iyidir
        visual.name = $"Cell_{x}_{y}";
    }

    // Grid çizgilerini çizmek için (Debug)
    public void DrawGizmos(int width, int height, float cellSize)
    {
        Gizmos.color = Color.gray;
        for (int x = 0; x <= width; x++)
            Gizmos.DrawLine(new Vector3(x * cellSize, 0), new Vector3(x * cellSize, height * cellSize));
        for (int y = 0; y <= height; y++)
            Gizmos.DrawLine(new Vector3(0, y * cellSize), new Vector3(width * cellSize, y * cellSize));
    }
}