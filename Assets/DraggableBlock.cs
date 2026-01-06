using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class DraggableBlock : MonoBehaviour
{
    [Header("Scale Settings")]
    public float slotScale = 0.5f; // Slotta dururkenki boyutu (Yarı yarıya)
    public float dragScale = 1.0f; // Sürüklerkenki boyutu (Orijinal)
    
    [Header("Settings")]
    [Tooltip("Blok parmağın ne kadar yukarısında dursun?")]
    public float dragOffsetY = 2f; // Bu senin 'x birim' dediğin değer
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
        
        // EKLENEN SATIR: Başlangıçta slot boyutuna getir
        transform.localScale = Vector3.one * slotScale;
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
        if (shape == null) return;

        // 1. ROTASYON (Herkes Dinliyor)
        // Artık "isDragging" kontrolünden ÖNCE olduğu için,
        // slotta duran bloklar da R'ye basınca dönecektir.
        if (Input.GetKeyDown(KeyCode.R))
        {
            shape.RotateRight(); // [cite: 75]
            RebuildVisual();     // [cite: 76]
            RebuildCollider();   // [cite: 64]
        }

        // 2. SÜRÜKLEME KONTROLÜ
        // Eğer bu blok şu an sürüklenmiyorsa, hareket kodlarını çalıştırma.
        if (!isDragging) return;

        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;

        // 2. Merkez Hesaplama (Az önce düzelttiğimiz kısım)
        float widthOffset = ((shape.Width - 1) * grid.cellSize) / 2f;
        float heightOffset = ((shape.Height - 1) * grid.cellSize) / 2f;
        
        Vector3 centerOffset = new Vector3(widthOffset, heightOffset, 0);

        // 3. YUKARI KAYDIRMA VE POZİSYONLAMA
        // Mouse - Merkez + Y_Kaydırma
        Vector3 liftOffset = new Vector3(0, dragOffsetY, 0);
        
        transform.position = mouse - centerOffset + liftOffset;

        // 4. Grid Kontrolleri (Değişmedi)
        // GridManager, bloğun sol-alt köşesini referans alır, o yüzden transform.position gönderiyoruz.
        Vector2Int cell = grid.WorldToCell(transform.position);
        
        bool canPlace = grid.CanPlace(shape, cell.x, cell.y);

        if (canPlace)
            ghost.Show(shape, cell, grid);
        else
            ghost.Clear();

        // Place (Bırakma)
        if (Input.GetMouseButtonUp(0))
        {
            if (canPlace)
            {
                // ... (yerleştirme kodları aynı) ...
                grid.PlacePiece(shape, cell.x, cell.y);
                ghost.Clear();
                Destroy(gameObject);
                BlockSpawner.Instance.OnBlockPlaced(this);
            }
            else
            {
                ghost.Clear();
                isDragging = false;
                transform.localPosition = Vector3.zero;
                
                // EKLENEN SATIR: Yerine geri döndüğünde tekrar küçülsün
                transform.localScale = Vector3.one * slotScale;
            }
        }
    }
    void OnMouseDown()
    {
        if (shape == null) return;
        isDragging = true;
        
        // EKLENEN SATIR: Tıklayınca orijinal boyuta büyüt
        transform.localScale = Vector3.one * dragScale;
    }

    void OnDisable()
    {
        if (ghost != null)
            ghost.Clear();
    }
}
