using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner Instance { get; private set; }

    [Header("Settings")]
    public List<BlockShapeSO> shapes;   // Tüm olası şekiller
    public DraggableBlock blockPrefab;
    public Transform[] slots;           // 3 adet spawn noktası

    private List<DraggableBlock> _activeBlocks = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnSet();
    }

    private void SpawnSet()
    {
        ClearActiveBlocks();

        if (shapes.Count == 0) return;

        // 1. Havuzun kopyasını oluştur (Orijinal listeyi bozmamak için)
        List<BlockShapeSO> pool = new List<BlockShapeSO>(shapes);

        // 2. Fisher-Yates Shuffle ile listeyi karıştır
        // (Tıpkı iskambil destesini karıştırır gibi)
        for (int i = 0; i < pool.Count; i++)
        {
            BlockShapeSO temp = pool[i];
            int randomIndex = Random.Range(i, pool.Count);
            pool[i] = pool[randomIndex];
            pool[randomIndex] = temp;
        }

        // 3. Karıştırılmış listeden sırasıyla çek
        for (int i = 0; i < slots.Length; i++)
        {
            // Eğer şekil sayın slot sayısından azsa (örn: 2 şekil var ama 3 slot var)
            // mecburen başa döner (Modülo işlemi).
            // Ama yeterli şeklin varsa hepsi benzersiz olur.
            BlockShapeSO uniqueShape = pool[i % pool.Count];
            
            SpawnOne(i, uniqueShape);
        }
        
        CheckGameOver();
    }

    // Artık şekli parametre olarak alıyor
    private void SpawnOne(int index, BlockShapeSO shapeToSpawn)
    {
        // Bloğu oluştur
        DraggableBlock block = Instantiate(blockPrefab, slots[index].position, Quaternion.identity);
        
        // Seçilen benzersiz şekli ver
        block.Initialize(shapeToSpawn);

        // Listeye ekle
        _activeBlocks.Add(block);
    }

    public void OnBlockPlaced(DraggableBlock block)
    {
        _activeBlocks.Remove(block);

        if (_activeBlocks.Count == 0)
        {
            SpawnSet();
        }
        else
        {
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        foreach (var block in _activeBlocks)
        {
            if (GridManager.Instance.CanFitAnywhere(block.GetData())) 
                return; 
        }

        Debug.Log("GAME OVER");
        GameManager.Instance.TriggerGameOver();
    }

    private void ClearActiveBlocks()
    {
        foreach (var b in _activeBlocks)
        {
            if (b != null) Destroy(b.gameObject);
        }
        _activeBlocks.Clear();
    }
}