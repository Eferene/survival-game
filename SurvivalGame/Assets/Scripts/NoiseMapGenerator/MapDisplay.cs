using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The renderer used to display the 2D noise or color map.")]
    [SerializeField] private Renderer textureRenderer;

    [Tooltip("The mesh filter component to hold the generated terrain mesh.")]
    [SerializeField] private MeshFilter meshFilter;

    [Tooltip("The mesh renderer component to render the generated terrain mesh.")]
    [SerializeField] private MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}