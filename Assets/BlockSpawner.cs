using System.Collections.Generic;
using UnityEngine;
public class BlockSpawner : MonoBehaviour
{
    public static BlockSpawner Instance { get; private set; }

    [Header("Shapes (SO listesi)")]
    public List<BlockShapeSO> shapes;

    [Header("Block Prefab")]
    public DraggableBlock blockPrefab;

    [Header("Spawn Slots (3 adet)")]
    public Transform[] slots;

    [Header("Grid")]
    public GridManager grid;

    List<DraggableBlock> activeBlocks = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnSet();
    }

    void SpawnSet()
    {
        ClearActive();

        for (int i = 0; i < 3; i++)
            SpawnOne(i);

        CheckGameOver();
    }

    void SpawnOne(int slotIndex)
    {
        BlockShapeSO so =
            shapes[Random.Range(0, shapes.Count)];

        BlockShape shape = new BlockShape();
        shape.cells = so.ToMatrix();
        shape.Trim(); // 🔴 BU SATIR KRİTİK

        DraggableBlock block =
            Instantiate(blockPrefab, slots[slotIndex].position, Quaternion.identity);

        block.SetShape(shape);
        activeBlocks.Add(block);
    }


    public void OnBlockPlaced(DraggableBlock block)
    {
        activeBlocks.Remove(block);
        Destroy(block.gameObject);

        if (activeBlocks.Count == 0)
            SpawnSet();
        else
            CheckGameOver();
    }

    void CheckGameOver()
    {
        foreach (var block in activeBlocks)
        {
            if (grid.CanFitAnywhere(block.Shape))
                return;
        }

        GameOver();
    }

    void GameOver()
    {
        Debug.Log("GAME OVER");
    }

    void ClearActive()
    {
        foreach (var b in activeBlocks)
            Destroy(b.gameObject);

        activeBlocks.Clear();
    }
}