#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralObjectPlacer))]
public class ProceduralObjectPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Varsayılan Inspector alanlarını çiz
        DrawDefaultInspector();

        // Scriptin referansını al
        ProceduralObjectPlacer placer = (ProceduralObjectPlacer)target;

        // Butonları ekle
        if (GUILayout.Button("Tüm Objeleri Oluştur", GUILayout.Height(25)))
        {
            placer.GenerateObjects();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Tüm Objeleri Temizle", GUILayout.Height(25)))
        {
            placer.ClearObjects();
        }
    }
}
#endif