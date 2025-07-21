#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    // DEĞİŞTİ: autoUpdate artık MapGenerator'ın kendi değişkeni değil, sadece editor'e özel.
    // Bu yüzden burada tanımlıyoruz.
    private bool autoUpdate;

    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator)target;

        if (DrawDefaultInspector())
        {
            if (autoUpdate)
            {
                // DEĞİŞTİ: Editörden yapılan değişiklikler event tetiklemesin.
                mapGenerator.GenerateIsland(false);
            }
        }

        EditorGUILayout.Space();

        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);

        if (GUILayout.Button("Generate World", GUILayout.Height(30)))
        {
            // DEĞİŞTİ: Buton da event tetiklemesin.
            mapGenerator.GenerateIsland(false);
        }
    }
}
#endif