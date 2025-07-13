// Bu kodun sadece Unity Editor'de derlenmesini sağlar. Build alırken dahil edilmez.
#if UNITY_EDITOR 
using UnityEditor;
using UnityEngine;

// Bu attribute, bu script'in MapGenerator component'inin Inspector'daki görünümünü
// özelleştireceğini Unity'e bildirir.
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    [SerializeField] private bool autoUpdate;

    // Inspector her yeniden çizildiğinde bu fonksiyon çalışır.
    public override void OnInspectorGUI()
    {
        // Hedef component'i (bizim durumumuzda MapGenerator) alıyoruz.
        MapGenerator mapGenerator = (MapGenerator)target;        

        // DrawDefaultInspector(), Inspector'daki tüm public değişkenleri varsayılan şekilde çizer.
        // Eğer bu çizim sırasında bir değer değişirse 'true' döner.
        if (DrawDefaultInspector())
        {
            if (autoUpdate)
            {
                // Haritayı yeniden oluştur.
                mapGenerator.GenerateWorld();
            }
        }

        EditorGUILayout.Space(); // Araya küçük bir boşluk ekle.

        // autoUpdate değişkeni için bir toggle (aç/kapa) butonu ekliyoruz.
        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);

        // "Generate World" butonu, manuel üretim için candır.
        if (GUILayout.Button("Generate World"))
        {
            // Haritayı yeniden oluştur.
            mapGenerator.GenerateWorld();
        }
    }
}
#endif