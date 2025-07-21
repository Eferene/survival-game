#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// Bu script, herhangi bir GameObject'in Inspector'ına buton ekler.
// Özellikle çok sayıda alt objesi olan parent'lar için kullanışlıdır.
[CustomEditor(typeof(Transform))]
[CanEditMultipleObjects]
public class ObjectContainerEditor : Editor
{
    private bool areChildrenHidden = false;

    public override void OnInspectorGUI()
    {
        // Varsayılan Transform Inspector'ını çiz.
        base.OnInspectorGUI();

        Transform targetTransform = (Transform)target;

        // Sadece 1'den fazla çocuğu olan objelerde bu butonu göster.
        if (targetTransform.childCount > 1)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hierarchy Tools", EditorStyles.boldLabel);

            // Butonun yazısını duruma göre değiştir (Gizle/Göster).
            string buttonText = areChildrenHidden ? "Show Children in Hierarchy" : "Hide Children in Hierarchy";

            if (GUILayout.Button(buttonText))
            {
                // Durumu tersine çevir.
                areChildrenHidden = !areChildrenHidden;

                // Bütün çocuk objelerin üzerinden geç.
                foreach (Transform child in targetTransform)
                {
                    if (areChildrenHidden)
                    {
                        // Objeyi Hierarchy'de gizle. Sahne'de hala görünür.
                        child.gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                    else
                    {
                        // Gizliliği kaldır.
                        child.gameObject.hideFlags = HideFlags.None;
                    }
                }
            }
        }
    }
}
#endif