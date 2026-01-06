using System.Collections.Generic;
using UnityEngine;
public class CellVisualPool : MonoBehaviour
{
    public static CellVisualPool Instance;

    [SerializeField] GameObject prefab;
    [SerializeField] int preload = 64;

    Queue<GameObject> pool = new();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < preload; i++)
            Create();
    }

    void Create()
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        pool.Enqueue(go);
    }

    public GameObject Get()
    {
        if (pool.Count == 0)
            Create();

        var go = pool.Dequeue();
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(transform);
        pool.Enqueue(go);
    }
}