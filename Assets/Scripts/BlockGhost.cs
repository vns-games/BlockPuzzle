using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public class BlockGhost : Singleton<BlockGhost>
{
    [Header("Settings")]
    public Transform cellsRoot;
    public GameObject cellPrefab;
    
    [Range(0f, 1f)] public float ghostAlpha = 0.5f;

    private float zOffset = -1f; 
    private List<GameObject> activeCells = new();

    // DEĞİŞİKLİK: 'Sprite visualSprite' parametresi eklendi
    public void Show(BlockData data, Vector2Int cell, GridManager grid, BlockColorType colorType, Sprite visualSprite)
    {
        Clear();
        float s = grid.cellSize;

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
                c.transform.localPosition = new Vector3(x * s, y * s, 0);

                SpriteRenderer sr = c.GetComponent<SpriteRenderer>();
                if (sr != null && visualSprite != null)
                {
                    // 1. Sürüklenen objenin resmini direkt buraya yapıştır
                    sr.sprite = visualSprite;
                    
                    // 2. Rengi elleme (Beyaz kalsın), sadece şeffaflık ver
                    sr.color = new Color(1f, 1f, 1f, ghostAlpha);
                }
            }
        }

        // 3. HIGHLIGHT MANAGER'A RESMİ GÖNDER
        // Grid'deki taşlar da bu resme dönüşsün
        if (GridHighlightManager.Instance && visualSprite != null)
        {
            GridHighlightManager.Instance.HighlightMatches(data, cell, visualSprite);
        }
    }

    public void Clear()
    {
        foreach (var c in activeCells) c.SetActive(false);

        if (GridHighlightManager.Instance) 
            GridHighlightManager.Instance.ClearHighlights();
    }

    GameObject GetCell()
    {
        foreach(var item in activeCells)
        {
            if (!item.activeSelf) { item.SetActive(true); return item; }
        }
        GameObject c = Instantiate(cellPrefab, cellsRoot);
        activeCells.Add(c);
        return c;
    }
}