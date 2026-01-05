using UnityEngine;

public class BlockController : MonoBehaviour
{
    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public int spawnIndex;
    private Vector3 offset;
    private bool dragging = false;

    private void OnMouseDown()
    {
        dragging = true;
        transform.localScale = Vector3.one; // Sürüklerken büyü
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        offset = transform.position - mousePos;
        GhostController.Instance.ShowGhost(transform, mousePos + offset);
    }

    private void OnMouseDrag()
    {
        if (!dragging) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        transform.position = mousePos + offset;
        GhostController.Instance.UpdateGhost(transform.position, transform);
    }

    private void OnMouseUp()
    {
        dragging = false;
        Vector3 snapPos = GhostController.Instance.CalculateSnapPosition(transform, transform.position);

        if (GhostController.Instance.CanPlaceBlock(snapPos, transform))
        {
            transform.position = snapPos;
            PlaceBlock();
        }
        else
        {
            transform.position = startPos;
            transform.localScale = Vector3.one * GridManager.Instance.previewScale;
        }
        GhostController.Instance.HideGhost();
    }

    void PlaceBlock()
    {
        GridManager gm = GridManager.Instance;
        // Parçaları Parent'tan ayırıp Grid'e ekle
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            Vector2Int coord = GhostController.WorldToGrid(child.position, gm.gridOrigin, gm.cellSize);
            
            child.SetParent(null); // Parent'tan çıkar
            gm.AddToGrid(coord.x, coord.y, child.gameObject);
        }

        gm.CheckLines(); // Patlama kontrolü
        gm.SpawnNewBlock(spawnIndex); // Yenisini getir
        Destroy(gameObject); // Boş kalan parent objeyi sil
    }
}