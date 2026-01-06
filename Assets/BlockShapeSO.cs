using UnityEditor;
using UnityEngine;
[CreateAssetMenu(menuName = "Puzzle/Block Shape")]
public class BlockShapeSO : ScriptableObject
{
    public int width = 3;
    public int height = 3;

    public bool[] cells; // width * height

    public void Resize()
    {
        if (cells == null || cells.Length != width * height)
            cells = new bool[width * height];
    }

    public bool Get(int x, int y)
    {
        return cells[y * width + x];
    }

    public void Set(int x, int y, bool value)
    {
        cells[y * width + x] = value;
    }
    
    public bool[,] ToMatrix()
    {
        bool[,] matrix = new bool[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                matrix[x, y] = cells[y * width + x];

        return matrix;
    }
}

[CustomEditor(typeof(BlockShapeSO))]
public class BlockShapeSOEditor : Editor
{
    const int CELL_SIZE = 22;

    public override void OnInspectorGUI()
    {
        BlockShapeSO shape = (BlockShapeSO)target;

        EditorGUI.BeginChangeCheck();

        shape.width = EditorGUILayout.IntField("Width", shape.width);
        shape.height = EditorGUILayout.IntField("Height", shape.height);

        shape.width = Mathf.Max(1, shape.width);
        shape.height = Mathf.Max(1, shape.height);

        shape.Resize();

        GUILayout.Space(10);
        DrawGrid(shape);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(shape);
        }
    }

    void DrawGrid(BlockShapeSO shape)
    {
        for (int y = 0; y < shape.height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < shape.width; x++)
            {
                bool value = shape.Get(x, y);
                bool newValue = GUILayout.Toggle(
                    value,
                    GUIContent.none,
                    GUILayout.Width(CELL_SIZE),
                    GUILayout.Height(CELL_SIZE)
                );

                if (newValue != value)
                    shape.Set(x, y, newValue);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
