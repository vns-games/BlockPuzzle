using UnityEngine;

public class GridBackgroundGenerator : MonoBehaviour
{
    [Header("Assets")]
    public GameObject cellBackgroundPrefab; // O siyah kare prefabını buraya at

    [Header("Settings")]
    public bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateBackground();
        }
    }

    public void GenerateBackground()
    {
        // GridManager'a ulaş (Singleton olduğu için kolay)
        GridManager gm = GridManager.Instance;
        if (gm == null || cellBackgroundPrefab == null) return;

        // Önce temizlik (Eğer editörde test edersen üst üste binmesin)
        foreach (Transform child in transform) 
        {
            Destroy(child.gameObject);
        }

        // Grid boyutlarına göre döngü
        for (int x = 0; x < gm.width; x++)
        {
            for (int y = 0; y < gm.height; y++)
            {
                // 1. Oluştur
                // transform parametresi vererek bu objenin (Generator) çocuğu yapıyoruz.
                // Böylece Hiyerarşi tertemiz kalıyor.
                GameObject bg = Instantiate(cellBackgroundPrefab, transform);
                
                // 2. Konumlandır
                // GridManager'ın koordinat sistemini kullanıyoruz
                bg.transform.position = gm.CellToWorld(x, y);
                
                // 3. İsimlendir (Debug kolaylığı)
                bg.name = $"BG_{x}_{y}";
            }
        }
    }
}