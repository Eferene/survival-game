#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralObjectPlacer))]
public class ProceduralObjectPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProceduralObjectPlacer placer = (ProceduralObjectPlacer)target;

        // DEĞİŞTİ: Buton artık sadece objeleri yerleştiriyor.
        if (GUILayout.Button("Place Objects on Current Map", GUILayout.Height(30)))
        {
            if (placer.mapGenerator != null)
            {
                // MapGenerator'ı tetiklemek yerine doğrudan yerleştirme fonksiyonunu çağırıyoruz.
                placer.PlaceObjects();
            }
            else
            {
                Debug.LogError("'Map Generator' referansı atanmamış!");
            }
        }

        if (GUILayout.Button("Clear Objects", GUILayout.Height(25)))
        {
            placer.ClearObjects();
        }
    }
}
#endif