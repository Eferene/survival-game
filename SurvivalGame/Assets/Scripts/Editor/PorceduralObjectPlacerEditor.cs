#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ProceduralObjectPlacer script'i için özel bir Inspector arayüzü oluşturur.
/// Adds some neat buttons to make our lives easier.
/// </summary>
[CustomEditor(typeof(ProceduralObjectPlacer))]
public class ProceduralObjectPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Varsayılan Inspector elemanlarını çiz (public değişkenler vb.).
        DrawDefaultInspector();

        // Hedef script'i al.
        ProceduralObjectPlacer placer = (ProceduralObjectPlacer)target;

        // "Place Objects on Current Map" adında, 30 piksel yüksekliğinde bir buton oluştur.
        if (GUILayout.Button("Place Objects on Current Map", GUILayout.Height(30)))
        {
            // Butona basıldığında, eğer MapGenerator referansı varsa...
            if (placer.mapGenerator != null)
            {
                // Doğrudan yerleştirme fonksiyonunu çağır.
                placer.PlaceObjects();
            }
            else
            {
                // Referans yoksa hata mesajı göster.
                Debug.LogError("'Map Generator' referansı atanmamış!");
            }
        }

        // "Clear Objects" adında, 25 piksel yüksekliğinde bir buton oluştur.
        if (GUILayout.Button("Clear Objects", GUILayout.Height(25)))
        {
            // Butona basıldığında temizleme fonksiyonunu çağır.
            placer.ClearObjects();
        }
    }
}
#endif