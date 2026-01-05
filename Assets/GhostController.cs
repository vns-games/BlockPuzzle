using UnityEngine;

public class GhostController : MonoBehaviour
{
    public static GhostController Instance;
    public Transform ghostParent; 

    private void Awake() => Instance = this;

    public void ShowGhost(Transform block, Vector3 initialPos)
    {
        ghostParent.gameObject.SetActive(true);
        
        // Ghost childlarını blok childlarıyla eşle
        for (int i = 0; i < ghostParent.childCount; i++)
        {
            Transform g = ghostParent.GetChild(i);
            if (i < block.childCount)
            {
                g.gameObject.SetActive(true);
                Transform orig = block.GetChild(i);
                g.localPosition = orig.localPosition;
                g.GetComponent<SpriteRenderer>().sprite = orig.GetComponent<SpriteRenderer>().sprite;
            }
            else g.gameObject.SetActive(false);
        }
        UpdateGhost(initialPos, block);
    }

    public void UpdateGhost(Vector3 blockPos, Transform block)
    {
        Vector3 snapPos = CalculateSnapPosition(block, blockPos);
        bool canPlace = CanPlaceBlock(snapPos, block);

        ghostParent.position = snapPos;
        ghostParent.rotation = block.rotation;

        Color c = canPlace ? new Color(1, 1, 1, 0.4f) : new Color(1, 0, 0, 0.4f);
        foreach (Transform g in ghostParent)
        {
            if(g.gameObject.activeSelf) g.GetComponent<SpriteRenderer>().color = c;
        }
    }

    public Vector3 CalculateSnapPosition(Transform block, Vector3 desiredPos)
    {
        GridManager gm = GridManager.Instance;
        float snappedX = Mathf.Round((desiredPos.x - gm.gridOrigin.x) / gm.cellSize) * gm.cellSize + gm.gridOrigin.x;
        float snappedY = Mathf.Round((desiredPos.y - gm.gridOrigin.y) / gm.cellSize) * gm.cellSize + gm.gridOrigin.y;
        return new Vector3(snappedX, snappedY, 0);
    }

    public bool CanPlaceBlock(Vector3 snapPos, Transform block)
    {
        GridManager gm = GridManager.Instance;
        foreach (Transform child in block)
        {
            // Hayalet yerleşse child nerede olurdu hesabı:
            Vector3 childWorld = snapPos + (block.rotation * child.localPosition);
            Vector2Int coord = WorldToGrid(childWorld, gm.gridOrigin, gm.cellSize);

            if (coord.x < 0 || coord.y < 0 || coord.x >= gm.gridWidth || coord.y >= gm.gridHeight) return false;
            if (gm.gridOccupied[coord.x, coord.y]) return false;
        }
        return true;
    }

    public static Vector2Int WorldToGrid(Vector3 worldPos, Vector2 origin, float cellSize)
    {
        // 0.01f eklemek float hassasiyet hatalarını (0.4999 gibi) önler
        int x = Mathf.RoundToInt((worldPos.x - origin.x + 0.01f) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - origin.y + 0.01f) / cellSize);
        return new Vector2Int(x, y);
    }

    public void HideGhost() => ghostParent.gameObject.SetActive(false);
}