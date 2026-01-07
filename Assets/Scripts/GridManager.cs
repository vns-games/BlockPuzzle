using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VnS.Utility.Singleton;

public class GridManager : Singleton<GridManager>
{
    public int width = 8;
    public int height = 8;
    public float cellSize = 1f;

    bool[,] grid;
    GameObject[,] visuals;

    protected override void Awake()
    {
        base.Awake();
        grid = new bool[width, height];
        visuals = new GameObject[width, height];
    }
    public bool CanFitAnywhere(BlockShape shape)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (CanPlace(shape, x, y))
                    return true;
            }
        }

        return false;
    }

    public bool CanPlace(BlockShape shape, int startX, int startY)
    {
        for (int x = 0; x < shape.Width; x++)
        {
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.cells[x, y]) continue;

                int gx = startX + x;
                int gy = startY + y;

                if (gx < 0 || gy < 0 || gx >= width || gy >= height)
                    return false;

                if (grid[gx, gy])
                    return false;
            }
        }
        return true;
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local =
            worldPos - transform.position;

        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.y / cellSize);

        return new Vector2Int(x, y);
    }
    
    List<int> GetFullRows()
    {
        List<int> rows = new();

        for (int y = 0; y < height; y++)
        {
            bool full = true;

            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y])
                {
                    full = false;
                    break;
                }
            }

            if (full)
                rows.Add(y);
        }

        return rows;
    }

    List<int> GetFullColumns()
    {
        List<int> cols = new();

        for (int x = 0; x < width; x++)
        {
            bool full = true;

            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y])
                {
                    full = false;
                    break;
                }
            }

            if (full)
                cols.Add(x);
        }

        return cols;
    }

    void ClearRow(int row)
    {
        for (int x = 0; x < width; x++)
            ClearCell(x, row);
    }

    void ClearColumn(int col)
    {
        for (int y = 0; y < height; y++)
            ClearCell(col, y);
    }

    void ClearCell(int x, int y)
    {
        grid[x, y] = false;

        if (visuals[x, y] != null)
        {
            CellVisualPool.Instance.Release(visuals[x, y]);
            visuals[x, y] = null;
        }
    }

    
    public void PlacePiece(BlockShape shape, int gx, int gy)
    {
        for (int x = 0; x < shape.Width; x++)
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.cells[x, y]) continue;

                int cx = gx + x;
                int cy = gy + y;

                grid[cx, cy] = true;

                var vis = CellVisualPool.Instance.Get();
                vis.transform.position = CellToWorld(cx, cy);
                visuals[cx, cy] = vis;
            }

        CheckAndClearLines();
    }
    void CheckAndClearLines()
    {
        var rows = GetFullRows();
        var cols = GetFullColumns();

        foreach (int r in rows)
            ClearRow(r);

        foreach (int c in cols)
            ClearColumn(c);
    }
    
    public Vector3 CellToWorld(int x, int y)
    {
        return transform.position +
               new Vector3(
                   (x + 0.5f) * cellSize,
                   (y + 0.5f) * cellSize,
                   0
               );
    }
    
    public bool GetCell(int x, int y)
    {
        return grid[x, y];
    }
    public Vector3 CellToWorldBottomLeft(Vector2Int cell)
    {
        return transform.position +
               new Vector3(
                   cell.x * cellSize,
                   cell.y * cellSize,
                   0
               );
    }
    
    void OnDrawGizmos()
    {
        if (grid == null)
            grid = new bool[width, height];

        // Grid çizgileri
        Gizmos.color = Color.blue;
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(x * cellSize, 0),
                transform.position + new Vector3(x * cellSize, height * cellSize)
            );
        }

        for (int y = 0; y <= height; y++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(0, y * cellSize),
                transform.position + new Vector3(width * cellSize, y * cellSize)
            );
        }

        // DOLU hücreler
        Gizmos.color = new Color(0, 1, 0, 0.35f);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y]) continue;

                Vector3 center = transform.position +
                                 new Vector3(
                                     (x + 0.5f) * cellSize,
                                     (y + 0.5f) * cellSize,
                                     0
                                 );

                Gizmos.DrawCube(center, Vector3.one * cellSize);
            }
        }
    }
    


}

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    const int CELL_SIZE = 18;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridManager grid = (GridManager)target;

        GUILayout.Space(10);
        GUILayout.Label("Grid Debug View", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Grid data play mode'da görünür",
                MessageType.Info
            );
            return;
        }

        for (int y = grid.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < grid.width; x++)
            {
                bool filled = grid.GetCell(x, y);

                GUI.color = filled ? Color.green : Color.gray;
                GUILayout.Box(
                    "",
                    GUILayout.Width(CELL_SIZE),
                    GUILayout.Height(CELL_SIZE)
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.color = Color.white;
    }
    
}