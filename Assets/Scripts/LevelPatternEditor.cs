#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(LevelPatternSO))]
public class LevelPatternEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LevelPatternSO pattern = (LevelPatternSO)target;

        // Standart alanları çiz (Renk vs.)
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Design", EditorStyles.boldLabel);

        // Grid boyutu değişirse diziyi güncelle
        int total = pattern.width * pattern.height;
        if (pattern.cells.Length != total)
        {
            System.Array.Resize(ref pattern.cells, total);
        }

        // --- IZGARA ÇİZİMİ ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Tabloyu ortala
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fixedWidth = 25;
        style.fixedHeight = 25;
        style.margin = new RectOffset(1, 1, 1, 1);

        for (int y = pattern.height - 1; y >= 0; y--) // Y ekseni aşağıdan yukarı değil, yukarıdan aşağı çizilsin diye ters döngü
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Ortalamak için
            for (int x = 0; x < pattern.width; x++)
            {
                bool val = pattern.Get(x, y);
                
                // Renk değişimi (Doluysa yeşil, boşsa gri)
                GUI.backgroundColor = val ? Color.green : Color.white;
                
                if (GUILayout.Button("", style))
                {
                    pattern.Set(x, y, !val);
                    EditorUtility.SetDirty(pattern); // Kaydetmesi için uyar
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        GUI.backgroundColor = Color.white; // Rengi normale döndür
        EditorGUILayout.EndVertical();

        // Temizle Butonu
        if (GUILayout.Button("Clear Grid"))
        {
            for (int i = 0; i < pattern.cells.Length; i++) pattern.cells[i] = false;
            EditorUtility.SetDirty(pattern);
        }
    }
}
#endif