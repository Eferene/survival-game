using UnityEngine;

/// <summary>
/// Bu struct, oluşturulan haritanın yükseklik verilerini (heightMap) tutar.
/// Diğer script'lerin bu veriye temiz bir şekilde erişmesini sağlar. That's the whole deal.
/// </summary>
public struct MapData
{
    // Sadece okunabilir bir iki boyutlu dizi, haritanın her noktasındaki yüksekliği saklar.
    public readonly float[,] heightMap;

    // Kurucu metot (Constructor), yeni bir MapData oluştururken heightMap'i atar.
    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}

public class MapGenerator : MonoBehaviour
{
    // --- Public Değişkenler (Inspector'da Görünür) ---

    [Header("Island Shape Settings")]
    [Tooltip("Oluşturulacak haritanın genişliği ve yüksekliği.")]
    public int mapSize = 257;
    [Tooltip("Adanın merkezden ne kadar uzağa yayılacağını belirler. Bu, adanın genel boyutunu kontrol eder.")]
    public float islandRadius = 100f;
    [Tooltip("Adanın kenarlarının ne kadar keskin veya yumuşak bir şekilde suya ineceğini belirler.")]
    public float islandFalloffPower = 2.5f;

    [Header("Noise Settings")]
    [Tooltip("Perlin Noise'un ölçeği. Düşük değerler daha yumuşak, büyük araziler; yüksek değerler daha sık ve detaylı tepeler oluşturur.")]
    public float noiseScale = 50f;
    [Tooltip("Detay seviyesi. Ne kadar çok oktav olursa, arazi o kadar karmaşık ve detaylı olur.")]
    public int octaves = 6;
    [Tooltip("Oktavlar arasındaki etki gücü. Düşük değerler araziyi düzleştirir, yüksek değerler daha pürüzlü yapar.")]
    [Range(0, 1)]
    public float persistence = 0.5f;
    [Tooltip("Oktavlar arasındaki frekans artışı. Detayların ne kadar sık olacağını belirler.")]
    public float lacunarity = 2f;
    [Tooltip("Noise haritasını x ve y ekseninde kaydırarak farklı arazi şekilleri bulmanı sağlar.")]
    public Vector2 offset;

    [Header("Mesh & Display Settings")]
    [Tooltip("Haritanın yüksekliğini çarpan değer. Arazinin ne kadar dağlık olacağını kontrol eder.")]
    public float meshHeightMultiplier = 25f;
    [Tooltip("Yükseklik verisini bir eğriye göre yeniden şekillendirir. Bu sayede platolar, dik yamaçlar gibi özel şekiller oluşturulabilir.")]
    public AnimationCurve meshHeightCurve;
    [Tooltip("Mesh'in detay seviyesi (Level of Detail). 0 en yüksek kalite, 6 en düşük kalite. Performans için önemlidir.")]
    [Range(0, 6)]
    public int levelOfDetail;
    [Tooltip("Oyun başladığında otomatik olarak yeni bir ada oluşturulsun mu?")]
    public bool autoGenerateOnStart = true;

    [Header("Terrain Coloring")]
    [Tooltip("Farklı yükseklikler için kullanılacak doku ve ayarları tanımlar. Water, sand, grass, rock, etc.")]
    public TerrainType[] regions;

    [Header("References & Internal Components")]
    [Tooltip("Oluşturulan mesh'i gösterecek olan renderer.")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [Tooltip("Oyuncuyu harita oluşturulduktan sonra doğru bir konuma yerleştirecek olan component.")]
    [SerializeField] private PlayerSpawnManager playerSpawner;

    // En son oluşturulan haritanın verisini tutar. Nullable (?) çünkü başlangıçta bir veri olmayabilir.
    private MapData? lastGeneratedMapData;
    // Ağaç, kaya gibi objeleri prosedürel olarak yerleştiren script.
    private ProceduralObjectPlacer proceduralObjectPlacer;


    // Bu struct, Inspector'da arazi bölgelerini (su, kum, çimen vb.) tanımlamak için kullanılır.
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        [Range(0, 1)] public float height;
        public Texture2D texture;
        public float tiling;
        [Tooltip("Geçişin merkezden sağa ve sola ne kadar yayılacağını belirler.")]
        [Range(0, 0.2f)] public float blendRange;
    }

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        proceduralObjectPlacer = GetComponent<ProceduralObjectPlacer>();

        if (autoGenerateOnStart)
        {
            GenerateIsland();
        }
    }

    /// <summary>
    /// Prosedürel olarak yeni bir ada haritası oluşturur ve ekrana çizer.
    /// </summary>
    public void GenerateIsland()
    {
        int seed = Random.Range(0, 100000);

        float[,] heightMap = Noise.GenerateNoiseMap(mapSize, mapSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        float[,] falloffMap = FalloffGenerator.GenerateSingleIslandFalloff(mapSize, islandRadius, islandFalloffPower);

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
            }
        }

        lastGeneratedMapData = new MapData(heightMap);

        // 1. Mesh'i oluştur ve ata
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        meshFilter.sharedMesh = meshData.CreateMesh();

        // 2. COLLIDER'I EKLE/GÜNCELLE (YENİ KISIM)
        // Obje üzerinde MeshCollider var mı diye bak, yoksa ekle.
        if (!TryGetComponent<MeshCollider>(out MeshCollider meshCollider))
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        // Yeni mesh'i collider'a ata. That's the ticket.
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // 3. Splat Map'i oluştur ve materyale uygula
        ApplyTerrainTextures(heightMap);

        // 4. Çevre objelerini yerleştir
        if (proceduralObjectPlacer != null && lastGeneratedMapData.HasValue)
        {
            proceduralObjectPlacer.HandleMapGeneration(lastGeneratedMapData.Value);
        }

        // 5. Oyuncuyu spawn et
        if (playerSpawner != null && Application.isPlaying)
        {
            playerSpawner.OnMapReady();
        }
    }

    /// <summary>
    /// Yükseklik haritasına göre Splatmap oluşturur ve materyal üzerine uygular.
    /// </summary>
    private void ApplyTerrainTextures(float[,] heightMap)
    {
        Texture2D splatMap = GenerateSplatMap(heightMap);
        Material terrainMaterial = meshRenderer.sharedMaterial;

        terrainMaterial.SetTexture("_Control", splatMap);

        // Dokuları ve tiling değerlerini döngü ile ata. Daha temiz, daha pratik.
        for (int i = 0; i < regions.Length && i < 4; i++) // Shader max 4 doku destekliyor.
        {
            terrainMaterial.SetTexture($"_Texture{i + 1}", regions[i].texture);
            terrainMaterial.SetFloat($"_Tile{i + 1}", regions[i].tiling);
        }
    }

    /// <summary>
    /// Yükseklik haritasına göre doku yoğunluklarını içeren bir Splat Map oluşturur.
    /// </summary>
    private Texture2D GenerateSplatMap(float[,] heightMap)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);
        Color[] splatMapColors = new Color[mapWidth * mapHeight];
        int regionCount = regions.Length;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = heightMap[x, y];
                float[] weights = new float[regionCount];

                // En baştan en yüksek bölgeyi varsayalım.
                if (regionCount > 0)
                {
                    weights[regionCount - 1] = 1f;
                }

                // Bölgeler arasında dolaşarak doğru ağırlığı veya geçişi bul.
                for (int i = 0; i < regionCount - 1; i++)
                {
                    TerrainType currentRegion = regions[i];
                    TerrainType nextRegion = regions[i + 1];

                    // Geçiş bölgesinin başlangıcını ve sonunu hesapla. This is the secret sauce.
                    float blendEnd = currentRegion.height + currentRegion.blendRange;

                    // Eğer yükseklik, mevcut bölgenin geçişinin altındaysa...
                    if (currentHeight <= blendEnd)
                    {
                        float blendStart = currentRegion.height - currentRegion.blendRange;

                        // Önce varsayılan en yüksek bölge ağırlığını sıfırla.
                        weights[regionCount - 1] = 0;

                        // ...ve geçiş aralığının içindeyse, ağırlıkları karıştır.
                        if (currentHeight >= blendStart)
                        {
                            // InverseLerp ile 0-1 arası bir geçiş faktörü al.
                            float t = Mathf.InverseLerp(blendStart, blendEnd, currentHeight);
                            weights[i] = 1 - t;
                            weights[i + 1] = t;
                        }
                        // ...değilse, bu bölgenin saf ağırlığını kullan (%100).
                        else
                        {
                            weights[i] = 1;
                        }
                        // Doğru aralığı bulduğumuz için döngüyü kırabiliriz. That's efficiency, baby.
                        break;
                    }
                }

                // Shader'ımız 4 doku destekliyor. Ağırlıkları renk kanallarına ata.
                float r = (regionCount > 0) ? weights[0] : 0;
                float g = (regionCount > 1) ? weights[1] : 0;
                float b = (regionCount > 2) ? weights[2] : 0;
                float a = (regionCount > 3) ? weights[3] : 0;

                splatMapColors[y * mapWidth + x] = new Color(r, g, b, a);
            }
        }
        return TextureGenerator.TextureFromColorMap(splatMapColors, mapWidth, mapHeight);
    }

    /// <summary>
    /// En son oluşturulan harita verisini döndürür.
    /// </summary>
    public MapData? GetLastGeneratedMapData()
    {
        return lastGeneratedMapData;
    }

    /// <summary>
    /// İki boyutlu bir haritada, tam pikseller arasına denk gelen bir noktanın yüksekliğini
    /// komşu pikselleri kullanarak yumuşak bir şekilde hesaplar (Bilinear Interpolation).
    /// BU METODU GERİ EKLEDİK.
    /// </summary>
    public static float GetBilinearInterpolatedHeight(float[,] heightMap, float u, float v)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);
        float x = u * (mapWidth - 1);
        float y = v * (mapHeight - 1);
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(x0 + 1, mapWidth - 1);
        int y1 = Mathf.Min(y0 + 1, mapHeight - 1);
        float tx = x - x0;
        float ty = y - y0;
        float h00 = heightMap[x0, y0];
        float h10 = heightMap[x1, y0];
        float h01 = heightMap[x0, y1];
        float h11 = heightMap[x1, y1];
        float a = Mathf.Lerp(h00, h10, tx);
        float b = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(a, b, ty);
    }
}