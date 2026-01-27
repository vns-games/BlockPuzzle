using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR

[CustomEditor(typeof(LevelPatternSO))]
public class LevelPatternEditor : Editor
{
    // O an seçili olan boya rengi
    private BlockColorType _selectedColor = BlockColorType.Red;

    // Silgi modu açık mı?
    private bool _isEraserMode = false;

    public override void OnInspectorGUI()
    {
        LevelPatternSO pattern = (LevelPatternSO)target;

        // 1. Boyut Ayarları
        EditorGUI.BeginChangeCheck();
        pattern.width = EditorGUILayout.IntField("Width", pattern.width);
        pattern.height = EditorGUILayout.IntField("Height", pattern.height);

        if (EditorGUI.EndChangeCheck())
        {
            pattern.ValidateArrays();
            EditorUtility.SetDirty(pattern);
        }

        GUILayout.Space(10);
        GUILayout.Label("🎨 Boyama Paleti", EditorStyles.boldLabel);

        // 2. Renk Seçim Butonları (Toolbar)
        DrawColorPalette();

        GUILayout.Space(10);

        // 3. Grid Çizimi
        DrawGrid(pattern);

        // Kaydetme butonu (Gerçi Unity otomatik kaydeder ama garanti olsun)
        if (GUILayout.Button("Force Save"))
        {
            EditorUtility.SetDirty(pattern);
            AssetDatabase.SaveAssets();
        }
    }

    void DrawColorPalette()
    {
        EditorGUILayout.BeginHorizontal();

        // Silgi Butonu
        GUI.backgroundColor = _isEraserMode ? Color.gray : Color.white;
        if (GUILayout.Button("Silgi", GUILayout.Height(30)))
        {
            _isEraserMode = true;
        }

        // Renk Butonları
        foreach (BlockColorType colorType in System.Enum.GetValues(typeof(BlockColorType)))
        {
            // Buton rengini ayarla (Görsel temsil için)
            GUI.backgroundColor = GetDebugColor(colorType);

            // Eğer seçiliyse isminin yanına tik koy
            string btnName = colorType.ToString();
            if (!_isEraserMode && _selectedColor == colorType) btnName = "✔ " + btnName;

            if (GUILayout.Button(btnName, GUILayout.Height(30)))
            {
                _selectedColor = colorType;
                _isEraserMode = false;
            }
        }
        GUI.backgroundColor = Color.white; // Rengi resetle
        EditorGUILayout.EndHorizontal();

        GUILayout.Label(_isEraserMode ? "Mod: SİLGİ" : $"Mod: BOYAMA ({_selectedColor})");
    }

    void DrawGrid(LevelPatternSO pattern)
    {
        pattern.ValidateArrays();

        for(int y = pattern.height - 1; y >= 0; y--) // Y eksenini ters çevirdik ki aşağıdan yukarı çizmesin
        {
            EditorGUILayout.BeginHorizontal();
            for(int x = 0; x < pattern.width; x++)
            {
                bool isActive = pattern.Get(x, y);
                BlockColorType cellColor = pattern.GetColor(x, y);

                // Eğer hücre doluysa o rengi göster, boşsa koyu gri yap
                GUI.backgroundColor = isActive ? GetDebugColor(cellColor) : new Color(0.2f, 0.2f, 0.2f);

                if (GUILayout.Button("", GUILayout.Width(30), GUILayout.Height(30)))
                {
                    Undo.RecordObject(pattern, "Paint Cell"); // Ctrl+Z desteği

                    if (_isEraserMode)
                    {
                        pattern.ClearCell(x, y);
                    }
                    else
                    {
                        // Hem aktif et hem rengi ata
                        pattern.Set(x, y, true, _selectedColor);
                    }
                    EditorUtility.SetDirty(pattern);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;
    }

    // Inspector'da butonların renkli görünmesi için basit bir çevirici
    private Color GetDebugColor(BlockColorType type)
    {
        switch(type)
        {
            case BlockColorType.Red: return new Color(1f, 0.4f, 0.4f);
            case BlockColorType.Blue: return new Color(0.4f, 0.6f, 1f);
            case BlockColorType.Green: return new Color(0.4f, 1f, 0.4f);
            case BlockColorType.Yellow: return Color.yellow;
            case BlockColorType.Purple: return new Color(0.8f, 0.4f, 1f);
            case BlockColorType.Cyan: return Color.cyan;
            case BlockColorType.Orange: return new Color(1f, 0.6f, 0f);
            case BlockColorType.Pink: return new Color(1f, 0.4f, 0.8f);
            default: return Color.white;
        }
    }
}

  #endif