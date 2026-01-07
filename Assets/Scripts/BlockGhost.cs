using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public class BlockGhost : Singleton<BlockGhost>
{
    public Transform cellsRoot;
    public GameObject cellPrefab;

    List<GameObject> activeCells = new();

    public void Show(BlockShape shape, Vector2Int cell, GridManager grid)
    {
        Clear();

        float s = grid.cellSize;

        // Transform pozisyon = grid CellToWorld (2 parametreli)
        Vector3 pos = grid.CellToWorld(cell.x, cell.y);
        pos.z = 0;
        transform.position = pos;

        for (int x = 0; x < shape.Width; x++)
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.cells[x, y]) continue;

                GameObject c = GetCell();
                c.transform.SetParent(cellsRoot, false);
                c.transform.localPosition = new Vector3(x * s, y * s, 0);
            }
    }

    GameObject GetCell()
    {
        GameObject c = Instantiate(cellPrefab);
        c.SetActive(true);
        activeCells.Add(c);
        return c;
    }

    public void Clear()
    {
        foreach (var c in activeCells)
            Destroy(c);
        activeCells.Clear();
    }
}