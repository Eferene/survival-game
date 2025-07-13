using UnityEngine;

// Bu sınıf, üretilen mesh ve texture verilerini sahnedeki nesnelere uygulamaktan sorumludur.
// Basically, it makes the stuff we generated actually show up on screen.
public class MapDisplay : MonoBehaviour
{
    // Inspector'dan atanacak olan sahne nesneleri.
    public Renderer textureRenderer; // 2D düzlemde texture göstermek için.
    public MeshFilter meshFilter;     // 3D mesh verisini tutmak için.
    public MeshRenderer meshRenderer; // Mesh'i renklendirmek ve materyal atamak için.

    // Sadece 2D bir texture çizmek için (örneğin height map'i debug amacıyla göstermek için).
    public void DrawTexture(Texture2D texture)
    {
        // ÖNEMLİ: Editörde çalışırken .material yerine .sharedMaterial kullan.
        // .material her seferinde yeni bir materyal kopyası oluşturur ve hafıza sızıntısına yol açar.
        // Don't be that guy.
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    // Hem 3D mesh'i hem de üzerine kaplanacak texture'ı çizmek için.
    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        // Yukarıdakiyle aynı sebeple .sharedMesh kullanıyoruz. Bu, editörde sızıntıyı önler.
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}