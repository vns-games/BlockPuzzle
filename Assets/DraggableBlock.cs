using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class DraggableBlock : MonoBehaviour
{
    public BlockShape Shape => shape;

    [Header("Visual")]
    public Transform visualRoot;
    public GameObject cellPrefab;

    BlockShape shape;
    PolygonCollider2D col;
    bool isDragging;

    private GridManager grid => GridManager.Instance;
    private BlockGhost ghost => BlockGhost.Instance;

    void Awake()
    {
        col = GetComponent<PolygonCollider2D>();
    }

    public void SetShape(BlockShape newShape)
    {
        shape = newShape;
        RebuildVisual();
        RebuildCollider();
    }

    void RebuildVisual()
    {
        foreach (Transform c in visualRoot)
            Destroy(c.gameObject);

        float s = grid.cellSize;

        for (int x = 0; x < shape.Width; x++)
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.cells[x, y]) continue;

                Vector3 pos = new Vector3(x * s, y * s, 0);
                Instantiate(cellPrefab, visualRoot).transform.localPosition = pos;
            }
    }

    void RebuildCollider()
    {
        List<Vector2[]> paths = new();
        float s = grid.cellSize;

        for (int x = 0; x < shape.Width; x++)
            for (int y = 0; y < shape.Height; y++)
            {
                if (!shape.cells[x, y]) continue;

                float px = x * s;
                float py = y * s;

                paths.Add(new Vector2[]
                {
                    new(px, py),
                    new(px + s, py),
                    new(px + s, py + s),
                    new(px, py + s)
                });
            }

        col.pathCount = paths.Count;
        for (int i = 0; i < paths.Count; i++)
            col.SetPath(i, paths[i]);
    }

    void Update()
    {
        if (!isDragging || shape == null) return;

        // Rotate
        if (Input.GetKeyDown(KeyCode.R))
        {
            shape.RotateRight();
            RebuildVisual();
            RebuildCollider();
        }

        // Mouse world
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
        transform.position = mouse;

        Vector2Int cell = grid.WorldToCell(mouse);
        bool canPlace = grid.CanPlace(shape, cell.x, cell.y);

        if (canPlace)
            ghost.Show(shape, cell, grid);
        else
            ghost.Clear();

        // Place
        if (Input.GetMouseButtonUp(0))
        {
            if (canPlace)
            {
                grid.PlacePiece(shape, cell.x, cell.y);
                ghost.Clear();
                Destroy(gameObject);
                BlockSpawner.Instance.OnBlockPlaced(this);
            }
            else
            {
                ghost.Clear();
                isDragging = false;
            }
        }
    }

    void OnMouseDown()
    {
        if (shape == null) return;
        isDragging = true;
    }

    void OnDisable()
    {
        if (ghost != null)
            ghost.Clear();
    }
}
