#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelPatternSO))]
public class LevelPatternEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Hedef nesneyi al
        LevelPatternSO pattern = (LevelPatternSO)target;

        // ScriptableObject güncellemelerini başlat
        serializedObject.Update();

        // Standart alanları çiz (Width, Height vs.)
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Design Visualizer", EditorStyles.boldLabel);

        // --- GÜVENLİK KONTROLLERİ ---
        // Genişlik veya Yükseklik 1'den küçük olamaz
        if (pattern.width < 1) pattern.width = 1;
        if (pattern.height < 1) pattern.height = 1;

        // Dizi boyutunu kontrol et ve gerekiyorsa güncelle
        int totalCells = pattern.width * pattern.height;
        if (pattern.cells == null || pattern.cells.Length != totalCells)
        {
            // Eğer dizi boyutu değişirse, veriyi kaybetmemek için Resize yapıyoruz
            // (Not: Genişlik değişirse kaydırma olabilir, ama basit resize veriyi korur)
            if (pattern.cells == null) pattern.cells = new bool[totalCells];
            else System.Array.Resize(ref pattern.cells, totalCells);
        }

        // --- IZGARA ÇİZİMİ ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Buton stilleri
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fixedWidth = 30;  // Tıklaması daha kolay olsun diye biraz büyüttüm
        style.fixedHeight = 30;
        style.margin = new RectOffset(2, 2, 2, 2);

        // Y ekseni döngüsü (Yukarıdan aşağıya çizmek için ters döngü)
        // Unity Koordinat Sistemi: (0,0) Sol Alt köşedir.
        // Bu döngü, Y ekseninin en üstünü (pattern.height - 1) editörün en tepesine çizer.
        // Böylece Editördeki görüntü ile Sahnedeki (Scene) görüntü eşleşir.
        for (int y = pattern.height - 1; y >= 0; y--) 
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Yatay ortalama

            for (int x = 0; x < pattern.width; x++)
            {
                // Mevcut değeri güvenli bir şekilde al
                bool currentValue = pattern.Get(x, y);
                
                // Renk ayarı: Dolu = Yeşil, Boş = Gri/Beyaz
                GUI.backgroundColor = currentValue ? new Color(0.2f, 1f, 0.2f) : Color.white;

                // Butonu çiz
                if (GUILayout.Button("", style))
                {
                    // --- KRİTİK NOKTA: UNDO KAYDI ---
                    // Değişiklik yapmadan önce Unity'nin Undo sistemine kaydet.
                    // Bu işlem hem CTRL+Z ile geri almayı sağlar hem de "SetDirty" işlemini
                    // Unity için en doğru şekilde yapar.
                    Undo.RecordObject(pattern, "Toggle Grid Cell");

                    // Değeri tersine çevir
                    pattern.Set(x, y, !currentValue);

                    // Değişikliği anında bildir
                    EditorUtility.SetDirty(pattern);
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        GUI.backgroundColor = Color.white; // Rengi sıfırla
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // --- TEMİZLEME BUTONU ---
        if (GUILayout.Button("Clear Grid", GUILayout.Height(30)))
        {
            Undo.RecordObject(pattern, "Clear Grid"); // Undo kaydı
            for (int i = 0; i < pattern.cells.Length; i++) 
            {
                pattern.cells[i] = false;
            }
            EditorUtility.SetDirty(pattern);
        }

        // Değişiklikleri uygula
        serializedObject.ApplyModifiedProperties();
    }
}
#endif