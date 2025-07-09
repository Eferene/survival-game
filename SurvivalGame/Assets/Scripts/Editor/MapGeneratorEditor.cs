using UnityEditor;
using UnityEngine;

// Bu script'in MapGenerator'ın Inspector'ını özelleştireceğini belirtiyoruz.
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Hedef component'i (MapGenerator) alıyoruz.
        MapGenerator mapGenerator = (MapGenerator)target;

        // DrawDefaultInspector() fonksiyonu, Inspector'da bir değer değiştiğinde 'true' döner.
        // Bu bizim tetikleyicimiz olacak.
        if (DrawDefaultInspector())
        {
            // Eğer bir değer değiştiyse VE autoUpdate açıksa...
            if (mapGenerator.autoUpdate)
            {
                // ...haritayı yeniden oluştur.
                mapGenerator.GenerateWorld();
            }
        }

        // Butonla diğer alanlar arasına biraz boşluk koyalım.
        EditorGUILayout.Space();

        // Manuel "Generate World" butonu her zaman lazım olabilir.
        if (GUILayout.Button("Generate World"))
        {
            mapGenerator.GenerateWorld();
        }
    }
}
