using UnityEngine;
using System.Collections.Generic;

public static class StaticCellPool
{
    private static Stack<VisualCell> _pool = new Stack<VisualCell>();
    private static VisualCell _prefab;
    private static Transform _poolRoot;

    private static void Init()
    {
        if (_prefab == null)
        {
            // Assets/Resources/VisualCell.prefab yolunu arar
            _prefab = Resources.Load<VisualCell>("VisualCell");
            if (_prefab == null)
            {
                Debug.LogError("HATA: 'Resources' klasöründe 'VisualCell' prefabı yok!");
                return;
            }
        }

        if (_poolRoot == null)
        {
            GameObject rootObj = new GameObject("--- STATIC_POOL_ROOT ---");
            Object.DontDestroyOnLoad(rootObj);
            _poolRoot = rootObj.transform;
        }
    }

    public static VisualCell Spawn(Vector3 position, Transform parent)
    {
        Init();

        var cell = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_prefab, parent);

        Transform t = cell.transform;
        t.position = position;
        t.localRotation = Quaternion.identity;

        cell.gameObject.SetActive(true);
        return cell;
    }

    public static void Despawn(VisualCell cell)
    {
        if (cell == null) return;

        cell.gameObject.SetActive(false);

        if (_poolRoot != null) cell.transform.SetParent(_poolRoot);

        _pool.Push(cell);
    }

    // --- DÜZELTME BURADA ---
    // Parametre tipi 'GameObject[,]' olarak değiştirildi.
    public static void ClearAllActive(GameObject[,] visualGrid, int w, int h)
    {
        for(int x = 0; x < w; x++)
        {
            for(int y = 0; y < h; y++)
            {
                GameObject obj = visualGrid[x, y];

                if (obj != null)
                {
                    // GameObject üzerinden VisualCell scriptini bul
                    VisualCell cell = obj.GetComponent<VisualCell>();

                    if (cell != null)
                    {
                        // Script varsa havuza at
                        Despawn(cell);
                    }
                    else
                    {
                        // Script yoksa (garip bir durum) direkt yok et
                        Object.Destroy(obj);
                    }

                    // Griddeki referansı temizle
                    visualGrid[x, y] = null;
                }
            }
        }
    }
}