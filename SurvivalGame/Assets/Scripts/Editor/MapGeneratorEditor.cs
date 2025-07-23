#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// MapGenerator script'inin Inspector'daki görünümünü özelleştirir.
/// Bu sayede butona basarak harita oluşturma gibi ek fonksiyonlar ekleyebiliriz.
/// </summary>
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    // Sadece Editor'de geçerli olacak bir değişken. Inspector'daki değer değiştikçe haritanın anında güncellenmesini sağlar.
    private bool autoUpdate;

    // Inspector arayüzünü çizmek için Unity tarafından çağrılan fonksiyon.
    public override void OnInspectorGUI()
    {
        // Hedef script olan MapGenerator'ı al.
        MapGenerator mapGenerator = (MapGenerator)target;

        // Varsayılan Inspector elemanlarını çiz. Eğer herhangi bir değer değişirse true döner.
        if (DrawDefaultInspector())
        {
            // Eğer "Auto Update" açıksa ve bir değer değiştiyse...
            if (autoUpdate)
            {
                mapGenerator.GenerateIsland();
            }
        }

        EditorGUILayout.Space();

        // Auto Update için bir toggle oluştur.
        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);

        if (GUILayout.Button("Generate World", GUILayout.Height(30)))
        {
            mapGenerator.GenerateIsland();
        }
    }
}
#endif