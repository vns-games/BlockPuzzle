using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
public class BlockVisuals : MonoBehaviour
{
    [Header("Visual Settings")]
    public Transform container;
    public GameObject cellPrefab;
    public float slotScale = 0.5f;
    public float dragScale = 1.0f;

    private List<GameObject> _activeCells = new();

    public void Redraw(BlockData shape, float cellSize)
    {
        // 1. Temizle
        foreach (var c in _activeCells) Destroy(c);
        _activeCells.Clear();

        // 2. Yeniden Oluştur
        for (int x = 0; x < shape.Width; x++)
        {
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.Matrix[x, y]) continue;

                GameObject cell = Instantiate(cellPrefab, container);
                cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0);
                _activeCells.Add(cell);
            }
        }
    }

    public void SetScale(bool isDragging)
    {
        float target = isDragging ? dragScale : slotScale;
        
        // DOTween varsa:
        transform.DOScale(target, 0.2f).SetEase(Ease.OutBack);
        
        // Yoksa:
        // transform.localScale = Vector3.one * target;
    }
}