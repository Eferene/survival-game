// Bu kodun sadece Unity Editor'de derlenmesini sağlar. Build alırken dahil edilmez.
#if UNITY_EDITOR 
using UnityEditor;
using UnityEngine;

// Bu attribute, bu script'in MapGenerator component'inin Inspector'daki görünümünü
// özelleştireceğini Unity'e bildirir.
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    // Inspector her yeniden çizildiğinde bu fonksiyon çalışır.
    public override void OnInspectorGUI()
    {
        // Hedef component'i (bizim durumumuzda MapGenerator) alıyoruz.
        MapGenerator mapGenerator = (MapGenerator)target;

        // DrawDefaultInspector(), Inspector'daki tüm public değişkenleri varsayılan şekilde çizer.
        // Eğer bu çizim sırasında bir değer değişirse 'true' döner.
        if (DrawDefaultInspector())
        {
            // Eğer MapGenerator script'indeki bir değer değiştiyse ve otomatik güncelleme açıksa...
            // Bu sayede Inspector'daki slider'ları kaydırırken anlık sonuç görürsün. Super useful.
            if (mapGenerator.autoGenerateOnStart)
            {
                // Haritayı yeniden oluştur.
                mapGenerator.GenerateWorld();
            }
        }

        EditorGUILayout.Space(); // Araya küçük bir boşluk ekle.

        // "Generate World" butonu, manuel üretim için candır.
        if (GUILayout.Button("Generate World"))
        {
            mapGenerator.GenerateWorld();
        }
    }
}
#endif