using UnityEngine;

public partial class GridManager
{
    private void OnDrawGizmos()
    {
        // Grid verisi yoksa (Editördeyken) geçici oluştur ki çizgileri görelim
        if (LevelGrid.Cells == null || LevelGrid.Cells.GetLength(0) != width || LevelGrid.Cells.GetLength(1) != height)
        {
            // Sadece çizim amaçlı
        }

        // 1. Grid Çizgileri
        Gizmos.color = Color.gray;
        for(int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(x * cellSize, 0),
                transform.position + new Vector3(x * cellSize, height * cellSize)
            );
        }
        for(int y = 0; y <= height; y++)
        {
            Gizmos.DrawLine(
                transform.position + new Vector3(0, y * cellSize),
                transform.position + new Vector3(width * cellSize, y * cellSize)
            );
        }

        // 2. Dolu Hücreler (Sadece Play modunda çalışır)
        if (Application.isPlaying && LevelGrid.Cells != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Yarı saydam kırmızı
            Vector3 size = Vector3.one * (cellSize * 0.9f);
            Vector3 offset = new Vector3(cellSize / 2f, cellSize / 2f, 0);

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    if (LevelGrid.Cells[x, y])
                    {
                        Vector3 pos = transform.position + new Vector3(x * cellSize, y * cellSize, 0) + offset;
                        Gizmos.DrawCube(pos, size);
                    }
                }
            }
        }
    }
}