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

// Bu component'in olduğu objeye otomatik olarak bir MapDisplay componenti eklenmesini zorunlu kılar.
[RequireComponent(typeof(MapDisplay))]
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
    [Tooltip("Zemin dokusunun çözünürlük çarpanı. 1, harita boyutuyla aynı çözünürlük demektir.")]
    [Range(1, 8)]
    public int textureResolutionMultiplier = 4;
    [Tooltip("Farklı yükseklikler için kullanılacak renk ve isimleri tanımlar. Water, sand, grass, rock, etc.")]
    public TerrainType[] regions;

    [Header("References & Internal Components")]
    [Tooltip("Oluşturulan mesh ve texture'ı ekranda gösterecek olan component.")]
    [SerializeField] private MapDisplay display;
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
        public Color color;
    }

    void Start()
    {
        if (display == null)
            display = GetComponent<MapDisplay>();

        // Bu objeye bağlı olan ProceduralObjectPlacer component'ini bul ve ata.
        proceduralObjectPlacer = GetComponent<ProceduralObjectPlacer>();

        // Eğer oyun başlangıcında otomatik harita oluşturma aktifse...
        if (autoGenerateOnStart)
        {
            // Ada oluşturma fonksiyonunu çağır.
            GenerateIsland();
        }
    }

    /// <summary>
    /// Prosedürel olarak yeni bir ada haritası oluşturur ve ekrana çizer.
    /// </summary>
    public void GenerateIsland()
    {
        // Rastgele bir seed değeri oluştur. Bu, her seferinde farklı bir harita üretilmesini sağlar.
        int seed = Random.Range(0, 100000);

        // Noise ve falloff haritalarını oluştur.
        float[,] heightMap = Noise.GenerateNoiseMap(mapSize, mapSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        float[,] falloffMap = FalloffGenerator.GenerateSingleIslandFalloff(mapSize, islandRadius, islandFalloffPower);

        // İki haritayı birleştirerek ada şeklini oluştur.
        // Bu döngü, haritanın her bir pikseli (noktası) üzerinden geçer.
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                // Noise haritasından falloff haritasını çıkararak kenarları suya batırıyoruz.
                // Clamp01 ile değerin 0 ve 1 arasında kalmasını sağlıyoruz.
                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
            }
        }

        // Oluşturulan son harita verisini sakla.
        lastGeneratedMapData = new MapData(heightMap);

        // Görselleştirmek için gerekli olan renk haritasını, mesh verisini ve dokuyu (texture) oluştur.
        Color[] colorMap = GenerateColorMap(heightMap);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        int textureSize = mapSize * textureResolutionMultiplier;
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, textureSize, textureSize);

        // MapDisplay script'ine bu verileri göndererek haritayı ekrana çizdir.
        display.DrawMesh(meshData, texture);

        // Eğer obje yerleştirici (ağaç, kaya vb.) varsa ve harita verisi mevcutsa...
        if (proceduralObjectPlacer != null && lastGeneratedMapData.HasValue)
        {
            // Obje yerleştiriciye haber ver, o da işini yapsın.
            proceduralObjectPlacer.HandleMapGeneration(lastGeneratedMapData.Value);
        }

        // Eğer oyuncu spawn yöneticisi varsa ve oyun çalışıyorsa (editörde değil)...
        if (playerSpawner != null && Application.isPlaying)
        {
            // Oyuncu spawn yöneticisine haritanın hazır olduğunu bildir. Time to spawn the player.
            playerSpawner.OnMapReady();
        }
    }

    /// <summary>
    /// En son oluşturulan harita verisini döndürür.
    /// </summary>
    public MapData? GetLastGeneratedMapData()
    {
        return lastGeneratedMapData;
    }

    /// <summary>
    /// Yükseklik haritasına ve 'regions' dizisine bakarak bir renk haritası oluşturur.
    /// </summary>
    private Color[] GenerateColorMap(float[,] heightMap)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);
        int texWidth = mapWidth * textureResolutionMultiplier;
        int texHeight = mapHeight * textureResolutionMultiplier;
        Color[] colorMap = new Color[texWidth * texHeight];

        // Renk haritasının her pikseli için döngüye gir.
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                // Daha pürüzsüz bir renk geçişi için mevcut pikselin yüksekliğini interpolasyon ile hesapla.
                float u = (float)x / (texWidth - 1);
                float v = (float)y / (texHeight - 1);
                float currentHeight = GetBilinearInterpolatedHeight(heightMap, u, v);

                // Tanımlanan regions arasında dolaşarak doğru rengi bul.
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        // Yüksekliğe uyan ilk bölgenin rengini ata ve döngüden çık.
                        colorMap[y * texWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }

    /// <summary>
    /// İki boyutlu bir haritada, tam pikseller arasına denk gelen bir noktanın yüksekliğini
    /// komşu pikselleri kullanarak yumuşak bir şekilde hesaplar (Bilinear Interpolation).
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