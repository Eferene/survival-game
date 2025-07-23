// Bu kod bloğunun sadece Unity Editor'de çalışmasını sağlar. Oyunda derlenmez.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// MapGenerator script'inin Inspector'daki görünümünü özelleştirir.
/// Bu sayede butona basarak harita oluşturma gibi ek fonksiyonlar ekleyebiliriz. It's all about workflow, my man.
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
                // Haritayı yeniden oluştur.
                mapGenerator.GenerateIsland();
            }
        }

        // Inspector'da biraz boşluk bırakır. For aesthetic reasons.
        EditorGUILayout.Space();

        // "Auto Update" için bir açma/kapama kutusu (Toggle) oluştur.
        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);

        // "Generate World" adında, 30 piksel yüksekliğinde bir buton oluştur.
        if (GUILayout.Button("Generate World", GUILayout.Height(30)))
        {
            // Butona basıldığında haritayı yeniden oluştur.
            mapGenerator.GenerateIsland();
        }
    }
}
#endif