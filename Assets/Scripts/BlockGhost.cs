using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public class BlockGhost : Singleton<BlockGhost>
{
    [Header("Settings")]
    public Transform cellsRoot;
    public GameObject cellPrefab;

    private float zOffset = -1f; // Ghost blokların altında kalsın diye
    private List<GameObject> activeCells = new();

    public void Show(BlockData data, Vector2Int cell, GridManager grid)
    {
        Clear();
        float s = grid.cellSize;

        // Hedef hücrenin merkezini alıyoruz
        Vector3 rootPos = grid.CellToWorld(cell.x, cell.y);
        rootPos.z = zOffset; 
        transform.position = rootPos;

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                if (!data.Matrix[x, y]) continue;

                GameObject c = GetCell();
                c.transform.SetParent(cellsRoot, false);
                // GridManager (0,0) noktasını hücrenin merkezi kabul ettiği için
                // local pozisyonları da buna göre ayarlıyoruz.
                c.transform.localPosition = new Vector3(x * s, y * s, 0);
            }
        }
    }

    GameObject GetCell()
    {
        foreach(var item in activeCells)
        {
            if (!item.activeSelf)
            {
                item.SetActive(true);
                return item;
            }
        }
        GameObject c = Instantiate(cellPrefab, cellsRoot);
        activeCells.Add(c);
        return c;
    }

    public void Clear()
    {
        foreach (var c in activeCells) c.SetActive(false);
    }
}