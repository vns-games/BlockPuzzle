using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Ayarları")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 0.5f;
    public Vector2 gridOrigin = new Vector2(-2, 0);

    [Header("Spawn Ayarları")]
    public Transform[] spawnPoints;
    public GameObject[] blockPrefabs;
    public float previewScale = 0.5f;

    public bool[,] gridOccupied;
    private GameObject[,] gridObjects; // Patlatırken silmek için objeleri tutar

    private void Awake()
    {
        Instance = this;
        gridOccupied = new bool[gridWidth, gridHeight];
        gridObjects = new GameObject[gridWidth, gridHeight];
    }

    private void Start() => SpawnInitialBlocks();

    void SpawnInitialBlocks()
    {
        for (int i = 0; i < spawnPoints.Length; i++) SpawnNewBlock(i);
    }

    [Header("Spawn Takibi")]
    private List<int> currentActivePrefabIndices = new List<int>() { -1, -1, -1 }; // 3 slot için prefab indexlerini tutar

    public void SpawnNewBlock(int index)
    {
        // Uygun olan prefabların indexlerini belirle (diğer 2 slotta olmayanlar)
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            // Eğer bu prefab indexi şu an diğer spawn noktalarında yoksa listeye ekle
            bool isAlreadyActive = false;
            for (int j = 0; j < currentActivePrefabIndices.Count; j++)
            {
                if (j != index && currentActivePrefabIndices[j] == i) 
                {
                    isAlreadyActive = true;
                    break;
                }
            }
            if (!isAlreadyActive) availableIndices.Add(i);
        }

        // Eğer tüm prefablar kullanımdaysa (az prefabınız varsa), mecburen rastgele seç
        int selectedPrefabIndex;
        if (availableIndices.Count > 0)
            selectedPrefabIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        else
            selectedPrefabIndex = Random.Range(0, blockPrefabs.Length);

        // Seçilen indexi kaydet
        currentActivePrefabIndices[index] = selectedPrefabIndex;

        // Bloğu oluştur
        GameObject newBlock = Instantiate(blockPrefabs[selectedPrefabIndex], spawnPoints[index].position, Quaternion.identity);
        newBlock.transform.localScale = Vector3.one * previewScale;
    
        BlockController bc = newBlock.GetComponent<BlockController>();
        bc.spawnIndex = index;
        bc.startPos = spawnPoints[index].position;
    }

    public void AddToGrid(int x, int y, GameObject obj)
    {
        gridOccupied[x, y] = true;
        gridObjects[x, y] = obj;
    }

    public void CheckLines()
    {
        List<int> rowsToClear = new List<int>();
        List<int> colsToClear = new List<int>();

        // Satırları kontrol et
        for (int y = 0; y < gridHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < gridWidth; x++) if (!gridOccupied[x, y]) full = false;
            if (full) rowsToClear.Add(y);
        }

        // Sütunları kontrol et
        for (int x = 0; x < gridWidth; x++)
        {
            bool full = true;
            for (int y = 0; y < gridHeight; y++) if (!gridOccupied[x, y]) full = false;
            if (full) colsToClear.Add(x);
        }

        // Patlatma işlemi
        foreach (int y in rowsToClear) ClearRow(y);
        foreach (int x in colsToClear) ClearColumn(x);
    }

    void ClearRow(int y)
    {
        for (int x = 0; x < gridWidth; x++) DeleteNode(x, y);
    }

    void ClearColumn(int x)
    {
        for (int y = 0; y < gridHeight; y++) DeleteNode(x, y);
    }

    void DeleteNode(int x, int y)
    {
        if (gridObjects[x, y] != null) Destroy(gridObjects[x, y]);
        gridOccupied[x, y] = false;
        gridObjects[x, y] = null;
    }
}