[System.Serializable]
public class BlockData
{
    public bool[,] Matrix { get; private set; }
    public int Width => Matrix.GetLength(0);
    public int Height => Matrix.GetLength(1);

    public BlockData(bool[,] matrix)
    {
        Matrix = matrix;
    }
}