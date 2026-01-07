using UnityEngine;
public class GridVisuals : MonoBehaviour
{
    private GameObject[,] _visuals;
    private GridManager _manager; // Veriye erişmek için referans

    public void Initialize(GridManager manager, int width, int height)
    {
        _manager = manager;
        _visuals = new GameObject[width, height];
    }

    public void AddVisual(int x, int y, Vector3 worldPos)
    {
        // Havuzdan çek ve yerleştir
        var vis = CellVisualPool.Instance.Get();
        vis.transform.position = worldPos;
        _visuals[x, y] = vis;
    }

    public void RemoveVisual(int x, int y)
    {
        if (_visuals[x, y] == null) return;

        // Efekt (Particle) eklenecekse burası en doğru yer
        // Instantiate(explosionFx, _visuals[x,y].transform.position, ...);

        CellVisualPool.Instance.Release(_visuals[x, y]);
        _visuals[x, y] = null;
    }

    private void OnDrawGizmos()
    {
        // Manager yoksa veya play mode dışında referans kayıpsa çizme
        if (_manager == null) _manager = GetComponent<GridManager>();
        if (_manager == null) return;

        // --- IZGARA ÇİZİMİ ---
        Gizmos.color = Color.yellow;
        int w = _manager.width;
        int h = _manager.height;
        float s = _manager.cellSize;

        for (int x = 0; x <= w; x++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(x * s, 0, 0),
                transform.position + new Vector3(x * s, h * s, 0)
            );
        }

        for (int y = 0; y <= h; y++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(0, y * s, 0),
                transform.position + new Vector3(w * s, y * s, 0)
            );
        }
    }
}