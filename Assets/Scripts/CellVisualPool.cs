using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.Singleton;

[DefaultExecutionOrder(-100)]
public class CellVisualPool : Singleton<CellVisualPool>
{

    [Header("Settings")]
    [SerializeField] private GameObject prefab; // VisualCell prefabı buraya
    [SerializeField] private int preload = 64;

    private Queue<GameObject> pool = new Queue<GameObject>();
    
    public void Initialize()
    {
        // Prefab kontrolü (Hata varsa burada yakalayalım)
        if (prefab == null)
        {
            Debug.LogError("KRİTİK HATA: CellVisualPool içindeki 'Prefab' slotu boş! Lütfen Inspector'dan atama yapın.");
            return;
        }

        for (int i = 0; i < preload; i++)
            Create();
    }

    GameObject Create()
    {
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, transform);
        go.SetActive(false);
        pool.Enqueue(go);
        return go;
    }

    public GameObject Get()
    {
        if (prefab == null)
        {
            Debug.LogError("Pool Hatası: Prefab yok!");
            return null;
        }

        GameObject go = null;

        // --- GÜVENLİK DÖNGÜSÜ ---
        // Havuzdan çektiğimiz obje "Destroy" edilmiş olabilir. 
        // Sağlam bir tane bulana kadar döngü kuruyoruz.
        while (go == null)
        {
            if (pool.Count == 0)
            {
                go = Create(); // Havuz bittiyse yenisini üret
                break;         // Create zaten sağlam döner
            }
            else
            {
                go = pool.Dequeue(); // Kuyruktan çek
            }
        }

        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        if (go == null) return;
        
        go.SetActive(false);
        go.transform.SetParent(transform); // Hiyerarşide Pool'un altına taşı
        pool.Enqueue(go);
    }
}