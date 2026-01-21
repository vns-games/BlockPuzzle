using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(BlockShapeSO))]
public class BlockShapeEditor : Editor
{
    private BlockShapeSO _target;

    private void OnEnable()
    {
        _target = (BlockShapeSO)target;
    }

    public override void OnInspectorGUI()
    {
        // Standart ScriptableObject başlığını çizmemesi için (isteğe bağlı)
        // DrawDefaultInspector(); 

        serializedObject.Update();

        // --- BAŞLIK ---
        EditorGUILayout.LabelField("Şekil Ayarları", EditorStyles.boldLabel);
        
        // --- BOYUT AYARLARI ---
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Genişlik (Width)", _target.width);
        int newHeight = EditorGUILayout.IntField("Yükseklik (Height)", _target.height);

        // Boyut değişirse array'i güvenli bir şekilde yeniden boyutlandır
        if (EditorGUI.EndChangeCheck())
        {
            if (newWidth < 1) newWidth = 1;
            if (newHeight < 1) newHeight = 1;

            Undo.RecordObject(_target, "Resize Shape"); // Geri alma (Ctrl+Z) desteği
            ResizeSafe(newWidth, newHeight);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Şekil Düzenleyici (Tıkla)", EditorStyles.boldLabel);

        // --- GÖRSEL IZGARA (GRID) ---
        // Y eksenini tersten çiziyoruz (Height-1'den 0'a) ki görsel olarak
        // Unity dünyasındaki Yukarı-Aşağı yönüyle eşleşsin.
        
        GUIStyle toggleStyle = new GUIStyle(GUI.skin.button);
        // Seçili kutular Yeşil, Boşlar Gri görünsün diye renk ayarı
        
        for (int y = _target.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < _target.width; x++)
            {
                bool currentState = _target.Get(x, y);
                
                // Renk değişimi: Doluysa Yeşil, Boşsa Normal
                GUI.backgroundColor = currentState ? Color.green : Color.white;

                if (GUILayout.Button("", GUILayout.Width(30), GUILayout.Height(30)))
                {
                    Undo.RecordObject(_target, "Toggle Cell");
                    _target.Set(x, y, !currentState);
                    EditorUtility.SetDirty(_target); // Kaydetmesi gerektiğini söyle
                }
                
                // Rengi normale döndür
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        // --- BİLGİ KUTUSU ---
        EditorGUILayout.Space(10);
        if (GUILayout.Button("Konsola Veriyi Yazdır"))
        {
            DebugShape();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Array'i verileri kaybetmeden yeniden boyutlandıran özel fonksiyon
    private void ResizeSafe(int newWidth, int newHeight)
    {
        bool[] newCells = new bool[newWidth * newHeight];

        // Eski verileri yeniye taşı (sınırlar dahilinde)
        for (int x = 0; x < Mathf.Min(_target.width, newWidth); x++)
        {
            for (int y = 0; y < Mathf.Min(_target.height, newHeight); y++)
            {
                // Eski array'den al
                bool val = _target.Get(x, y);
                // Yeni array'e koy
                newCells[y * newWidth + x] = val;
            }
        }

        _target.width = newWidth;
        _target.height = newHeight;
        _target.cells = newCells;
        
        EditorUtility.SetDirty(_target);
    }

    private void DebugShape()
    {
        string log = $"Shape: {_target.name} ({_target.width}x{_target.height})\n";
        for (int y = _target.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < _target.width; x++)
            {
                log += _target.Get(x, y) ? "[1] " : "[0] ";
            }
            log += "\n";
        }
        Debug.Log(log);
    }
}