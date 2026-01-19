using UnityEngine;
using VnS.Utility.Singleton;

public class ParticleManager : Singleton<ParticleManager>
{
    [Header("Setup")]
    [SerializeField] private CellParticle particlePrefab;

    // Griddeki her bir hücre için özel bir partikül tutuyoruz
    private CellParticle[,] _particleGrid;

    // GridManager başlatıldıktan sonra çağrılmalı (Start veya Init içinde)
    public void Initialize(int width, int height)
    {
        // Eğer zaten oluşturduysak tekrar yapma
        if (_particleGrid != null) return;

        _particleGrid = new CellParticle[width, height];

        // Düzenli dursun diye bir parent obje
        GameObject holder = new GameObject("Particles_Grid_Fixed");
        holder.transform.SetParent(this.transform);

        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                // 1. Yarat
                CellParticle p = Instantiate(particlePrefab, holder.transform);

                // 2. Griddeki yerine yerleştir
                // NOT: GridManager'ın dünya koordinatlarını nasıl hesapladığını bildiğini varsayıyoruz.
                // Eğer GridManager'da 'GetWorldPosition(x,y)' varsa onu kullan.
                // Yoksa kendi hesaplamanı buraya yaz.
                p.transform.position = GridManager.Instance.CellToWorld(x, y);

                // 3. Diziye kaydet
                _particleGrid[x, y] = p;
            }
        }
    }

    public void PlayEffect(int x, int y, BlockColorType type)
    {
        // Grid sınırlarını kontrol et
        if (_particleGrid != null &&
            x >= 0 && x < _particleGrid.GetLength(0) &&
            y >= 0 && y < _particleGrid.GetLength(1))
        {
            // İlgili koordinattaki partikülü bul ve patlat
            _particleGrid[x, y].Play(type);
        }
    }
}