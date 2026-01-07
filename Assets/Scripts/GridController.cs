using UnityEngine;
using VnS.Utility.Singleton;
public class GridController : Singleton<GridController>
{
    public GridSettingsSO settings;
    public GridVisual gridVisual;

    private bool[,] _gridData; // Saf Data

    protected override void Awake()
    {
        base.Awake();
        _gridData = new bool[settings.columns, settings.rows];
    }

    // Dünyadaki pozisyonu grid indeksine çevirir
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        int x = Mathf.FloorToInt(local.x / settings.cellSize);
        int y = Mathf.FloorToInt(local.y / settings.cellSize);
        return new Vector2Int(x, y);
    }

    public bool TryPlaceShape(BlockData shape, Vector3 worldPos)
    {
        // Bloğun sol alt köşesini hesapla (Mover'daki offset hesabının tersi)
        // Çünkü worldPos bloğun merkezi, grid sol altı ister.
        // Basitleştirmek için: BlockController'dan sol alt köşeyi isteyebiliriz 
        // veya direkt shape pivotunu sol alt kabul edip görseli kaydırabiliriz.
        // Şimdilik mevcut mantığını koruyarak:
        
        Vector2Int cell = WorldToCell(worldPos);

        // Extension Method kullanımı:
        if (_gridData.CanPlaceShape(shape, cell.x, cell.y))
        {
            PlaceShapeLogic(shape, cell);
            return true;
        }
        return false;
    }

    private void PlaceShapeLogic(BlockData shape, Vector2Int startCell)
    {
        for (int x = 0; x < shape.Width; x++)
        {
            for (int y = 0; y < shape.Height; y++)
            {
                if (shape.Matrix[x, y])
                {
                    int gx = startCell.x + x;
                    int gy = startCell.y + y;
                    
                    _gridData[gx, gy] = true; // Data güncelle
                    
                    // Görsel oluştur (Visual)
                    gridVisual.PlaceCellVisual(null, gx, gy, settings.cellSize, transform); // null yerine prefab gelmeli
                }
            }
        }
        
        // Satır silme kontrolü (Logic) buraya eklenecek
        CheckLines();
    }

    private void CheckLines()
    {
        // Burayı da Extension metoduna çevirebiliriz: _gridData.GetFullRows()
        // Sonra temizleyip puan veririz.
    }
    
    private void OnDrawGizmos()
    {
        if(settings != null)
            new GridVisual().DrawGizmos(settings.columns, settings.rows, settings.cellSize);
    }
}