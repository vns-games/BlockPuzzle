using UnityEngine;

public class DragPointGizmo : MonoBehaviour
{
    [Header("Gizmo Ayarları")]
    [Tooltip("True ise sadece mouse sol tuş basılıyken gösterir.")]
    public bool showOnlyWhenClicking = true; 
    public Color pointerColor = Color.red;      // İşaretçi rengi
    [Range(0.1f, 2f)]
    public float pointerSize = 0.5f;            // Dış halkanın boyutu
    
    [Header("Mesafe Ayarı")]
    [Tooltip("Raycast bir yere çarpmazsa bu kadar uzakta gösterilir.")]
    public float defaultDistance = 10f;      

    // Hesaplanan pozisyonu burada tutacağız
    private Vector3 currentWorldPos;
    private bool isActive = false;

    void Update()
    {
        // Eğer "Sadece Tıklarken" seçiliyse sol tık kontrolü yap
        if (showOnlyWhenClicking)
        {
            isActive = Input.GetMouseButton(0); // Sol tık basılı olduğu sürece true
        }
        else
        {
            isActive = true; // Her zaman göster
        }

        if (isActive)
        {
            CalculateMousePosition();
        }
    }

    void CalculateMousePosition()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        // 1. Zemin gibi fiziksel bir yüzeye çarpıyorsa orayı göster
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            currentWorldPos = hit.point;
        }
        // 2. Boşlukta ise varsayılan mesafeyi kullan
        else
        {
            currentWorldPos = ray.GetPoint(defaultDistance);
        }
    }

    void OnDrawGizmos()
    {
        // Oyun çalışmıyorken veya tık basılı değilse çizme
        if (!Application.isPlaying || !isActive) return;

        Gizmos.color = pointerColor;

        // Hedef noktayı belirginleştirmek için iç içe iki küre
        
        // 1. Dışarıya yarı şeffaf, tel kafes bir halka
        Gizmos.DrawWireSphere(currentWorldPos, pointerSize);
        
        // 2. Tam merkeze küçük, dolu bir nokta
        Gizmos.DrawSphere(currentWorldPos, pointerSize * 0.25f);
    }
}