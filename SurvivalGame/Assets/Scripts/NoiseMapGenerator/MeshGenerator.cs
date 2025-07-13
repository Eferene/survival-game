using UnityEngine;

// Bu statik sınıf, bir yükseklik haritasından (float[,]) 3D bir arazi modeli (Mesh) oluşturur.
public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);

        // LOD (Level of Detail) için atlama miktarı. 0 ise 1, 1 ise 2, 2 ise 4...
        // Bu, daha az vertex kullanarak daha basit (daha performanslı) bir mesh oluşturmamızı sağlar.
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        // GÜVENLİK KONTROLÜ: Harita boyutunun LOD'a bölünebilir olduğundan emin ol.
        // Yoksa çökersin bro. (mapWidth - 1) değeri, LOD atlama miktarına tam bölünmelidir.
        if ((mapWidth - 1) % meshSimplificationIncrement != 0 || (mapHeight - 1) % meshSimplificationIncrement != 0)
        {
            Debug.LogError($"Harita boyutu ({mapWidth - 1}) seçilen LOD ({levelOfDetail}) ile uyumlu değil.");
            return new MeshData(0, 0); // Boş mesh data döndürerek hatayı engelle.
        }

        // LOD'a göre bir sırada kaç vertex olacağını hesapla.
        int verticesPerLine = (mapWidth - 1) / meshSimplificationIncrement + 1;
        // Mesh'i dünyanın merkezine (0,0,0) yerleştirmek için başlangıç pozisyonunu hesapla.
        float topLeftX = (mapWidth - 1) / -2f;
        float topLeftZ = (mapHeight - 1) / 2f;

        // Vertex ve triangle bilgilerini tutacak olan veri yapısını oluştur.
        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        // Eş zamanlı (multi-threading) kullanımda sorun çıkmaması için AnimationCurve'ün bir kopyasını oluştur.
        // A good habit to get into.
        AnimationCurve threadSafeHeightCurve = new AnimationCurve(heightCurve.keys);

        // Yükseklik haritası üzerinde, LOD atlama miktarına göre ilerleyerek vertex'leri oluştur.
        for (int y = 0; y < mapHeight; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < mapWidth; x += meshSimplificationIncrement)
            {
                // Yüksekliği al, curve ile yeniden şekillendir ve multiplier ile çarp.
                float currentHeight = threadSafeHeightCurve.Evaluate(heightMap[x, y]);
                // Vertex pozisyonunu ve UV (texture koordinatı) bilgisini ata.
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, currentHeight * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)mapWidth, y / (float)mapHeight);

                // Eğer haritanın sağ ve alt kenarında değilsek, üçgenleri oluştur.
                if (x < mapWidth - 1 && y < mapHeight - 1)
                {
                    // Bir kare (quad) oluşturmak için iki üçgen ekle.
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                vertexIndex++;
            }
        }
        return meshData;
    }
}

// Bir mesh oluşturmak için gereken tüm verileri (vertex'ler, üçgenler, UV'ler) tutan basit bir sınıf.
// Bu, verileri thread'ler arasında güvenle taşımayı kolaylaştırır.
public class MeshData
{
    public Vector3[] vertices; // 3D uzaydaki noktalar.
    public int[] triangles;    // Vertex'leri birleştirerek üçgen yüzeyler oluşturan index'ler.
    public Vector2[] uvs;      // Texture'ın mesh üzerine nasıl kaplanacağını belirleyen koordinatlar.
    int triangleIndex;         // Bir sonraki üçgenin nereye ekleneceğini takip eder.

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        // Bir kare 2 üçgenden, her üçgen 3 vertex'ten oluşur, o yüzden 6 ile çarpıyoruz.
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    // Üçgen dizisine 3 vertex index'i ekleyerek bir üçgen oluşturur.
    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    // Bu veri yapısındaki bilgilerle gerçek bir Unity Mesh nesnesi oluşturur.
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();

        // Unity'nin varsayılan 16-bit vertex index limiti ~65k'dır.
        // Daha büyük mesh'ler için index formatını 32-bit'e çıkarmamız gerekir.
        // This is a must for high-res worlds.
        if (vertices.Length > 65534)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        // Işıklandırmanın doğru çalışması için normalleri (yüzey yönlerini) yeniden hesapla.
        mesh.RecalculateNormals();
        return mesh;
    }
}