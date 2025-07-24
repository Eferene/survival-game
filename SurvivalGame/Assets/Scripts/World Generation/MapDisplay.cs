using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        // Gerekli component'leri al veya yoksa ekle.
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

        // Mesh'i oluştur ve ata.
        Mesh mesh = meshData.CreateMesh();
        meshFilter.sharedMesh = mesh;

        // Materyal sızıntısını önleyen kod.
        Material sourceMat = meshRenderer.sharedMaterial;
        if (sourceMat == null)
        {
            sourceMat = new Material(Shader.Find("Standard"));
        }

        Material materialInstance = new Material(sourceMat);
        materialInstance.mainTexture = texture;
        meshRenderer.sharedMaterial = materialInstance;

        // Collider'a da mesh'i ata.
        meshCollider.sharedMesh = mesh;
    }
}