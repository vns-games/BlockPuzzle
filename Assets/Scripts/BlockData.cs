using UnityEngine;
public class BlockData : MonoBehaviour
{
    public BlockShapeSO shapeSO;

    public BlockShape GetShape()
    {
        return new BlockShape
        {
            cells = ToMatrix(shapeSO)
        };
    }

    bool[,] ToMatrix(BlockShapeSO so)
    {
        bool[,] matrix = new bool[so.width, so.height];

        for (int y = 0; y < so.height; y++)
            for (int x = 0; x < so.width; x++)
                matrix[x, y] = so.Get(x, y);

        return matrix;
    }
}