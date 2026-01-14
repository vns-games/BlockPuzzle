using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

public class GridHighlightManager : Singleton<GridHighlightManager>
{
    private struct HighlightHistory
    {
        public SpriteRenderer renderer;
        public Color originalColor;
        public Sprite originalSprite; 
    }

    private List<HighlightHistory> _activeHighlights = new List<HighlightHistory>();

    public void HighlightMatches(BlockData ghostData, Vector2Int ghostPos, Sprite ghostSprite)
    {
        ClearHighlights();

        Grid grid = GridManager.Instance.LevelGrid;
        if (grid == null || grid.Visuals == null) return;

        int width = grid.Width;
        int height = grid.Height;

        List<int> fullRows = new List<int>();
        List<int> fullCols = new List<int>();

        // --- SİMÜLASYON ---
        for (int y = 0; y < height; y++)
        {
            bool isRowFull = true;
            for (int x = 0; x < width; x++)
            {
                bool occupied = grid.Cells[x, y];
                if (!occupied)
                {
                    int lx = x - ghostPos.x;
                    int ly = y - ghostPos.y;
                    if (lx >= 0 && lx < ghostData.Width && ly >= 0 && ly < ghostData.Height && ghostData.Matrix[lx, ly]) occupied = true;
                }
                if (!occupied) { isRowFull = false; break; }
            }
            if (isRowFull) fullRows.Add(y);
        }

        for (int x = 0; x < width; x++)
        {
            bool isColFull = true;
            for (int y = 0; y < height; y++)
            {
                bool occupied = grid.Cells[x, y];
                if (!occupied)
                {
                    int lx = x - ghostPos.x;
                    int ly = y - ghostPos.y;
                    if (lx >= 0 && lx < ghostData.Width && ly >= 0 && ly < ghostData.Height && ghostData.Matrix[lx, ly]) occupied = true;
                }
                if (!occupied) { isColFull = false; break; }
            }
            if (isColFull) fullCols.Add(x);
        }

        // --- DEĞİŞTİRME ---
        Color targetColor = new Color(1f, 1f, 1f);

        foreach (int y in fullRows)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject visObj = grid.Visuals[x, y];
                if (visObj != null) SwapVisuals(visObj, ghostSprite, targetColor);
            }
        }

        foreach (int x in fullCols)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject visObj = grid.Visuals[x, y];
                if (visObj != null) SwapVisuals(visObj, ghostSprite, targetColor);
            }
        }
    }

    private void SwapVisuals(GameObject obj, Sprite newSprite, Color newColor)
    {
        var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (sr.gameObject.name.Contains("Glow")) continue;
            if (_activeHighlights.Exists(h => h.renderer == sr)) continue;

            _activeHighlights.Add(new HighlightHistory 
            { 
                renderer = sr, 
                originalColor = sr.color,
                originalSprite = sr.sprite 
            });

            sr.sprite = newSprite;
            sr.color = newColor;
        }
    }

    public void ClearHighlights()
    {
        foreach (var h in _activeHighlights)
        {
            if (h.renderer != null)
            {
                // --- DÜZELTME BURADA ---
                // 'sr' yerine 'h.renderer' kullanıyoruz
                h.renderer.sprite = h.originalSprite; 
                h.renderer.color = h.originalColor;   
            }
        }
        _activeHighlights.Clear();
    }
}