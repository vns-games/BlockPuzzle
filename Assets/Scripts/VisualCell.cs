using UnityEngine;
using UnityEngine.Events;

public class VisualCell : MonoBehaviour
{
    [Header("Main Visual")]
    public SpriteRenderer mainRenderer;

    [Header("Events")]
    public UnityEvent OnIdleEvent;     // Dururken ne olsun? (Gölge aç, Glow kapa vs.)
    public UnityEvent OnDraggingEvent; // Taşınırken ne olsun? (Glow aç, Büyüt vs.)
    public UnityEvent OnDropEvent;     // Bırakılınca ne olsun? (Partikül patlat vs.)

    // DraggableBlock burayı çağırıp resmi atayacak
    public void Initialize(Sprite sprite, int sortingOrder)
    {
        if (mainRenderer)
        {
            mainRenderer.sprite = sprite;
            mainRenderer.sortingOrder = sortingOrder;
        }

        // Başlangıç durumu
        OnIdle();
    }

    // --- TETİKLEYİCİLER ---

    public void OnIdle()
    {
        OnIdleEvent?.Invoke();
    }

    public void OnDragging()
    {
        OnDraggingEvent?.Invoke();
    }

    public void OnDrop()
    {
        OnDropEvent?.Invoke();
    }
}