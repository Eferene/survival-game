using UnityEngine;
using System.Collections.Generic;

// Bu script'in çalışması için MapDisplay component'inin aynı GameObject'te bulunmasını zorunlu kılar.
// Eğer yoksa, Unity otomatik olarak ekler. It's a lifesaver, trust me.
[RequireComponent(typeof(MapDisplay))]
// Harita üretiminin beyni. Bütün ayarları buradan yapıp, üretimi tetikleriz.
public class MapGenerator : MonoBehaviour
{
    // --- INSPECTOR AYARLARI ---
    // Bu değişkenler Unity Editor'deki Inspector panelinde görünür ve buradan ayarlanabilir.

    [Header("World Settings")] // Dünyanın temel boyut ve su seviyesi ayarları
    public int worldSize = 513; // Haritanın kare olarak boyutu (genellikle 2^n + 1 kullanılır)
    [Range(0, 1)]
    public float waterLevel = 0.35f; // Su seviyesinin ne kadar yüksek olacağı (0-1 arası)

    [Header("Island Generation")] // Ada oluşturma parametreleri
    public int numberOfIslands = 5; // Ana ada dahil toplam ada sayısı
    public float islandRadius = 100f; // Adaların temel yarıçapı
    public float mainIslandSizeMultiplier = 2f; // Ana adanın diğerlerine göre ne kadar büyük olacağı
    public float islandFalloffPower = 2.5f; // Ada kenarlarının ne kadar keskin veya yumuşak olacağı
    [Tooltip("Uyduların ana adadan en az ne kadar uzakta olacağı.")]
    public float minSpawnRadius = 150f;
    [Tooltip("Uyduların ana adadan en fazla ne kadar uzakta olacağı.")]
    public float maxSpawnRadius = 220f;
    [Tooltip("Adaların birbirine ne kadar yakın olabileceği.")]
    public float minIslandDistance = 100f;

    [Header("Noise Settings")] // Perlin Noise (arazi şekilleri) ayarları
    public int seed; // Rastgele harita üretimi için tohum. Aynı seed, aynı haritayı verir.
    public float noiseScale = 50f; // Gürültü haritasının ölçeği/yakınlığı. Düşük değer = daha yakın/detaylı.
    public int octaves = 6; // Detay seviyesi. Daha fazla oktav, daha fazla detay ama daha fazla işlem gücü demek.
    [Range(0, 1)]
    public float persistence = 0.5f; // Oktavların genel şekle etkisinin ne kadar süreceği.
    public float lacunarity = 2f; // Oktavların detay seviyesinin ne kadar artacağı.
    public Vector2 offset; // Haritayı x ve y ekseninde kaydırmak için.

    [Header("Mesh & Display")] // 3D model ve görünüm ayarları
    public float meshHeightMultiplier = 25f; // Arazinin ne kadar dağlık olacağı.
    public AnimationCurve meshHeightCurve; // Yüksekliklerin dağılımını bir eğriyle kontrol etmemizi sağlar.
    [Range(0, 6)]
    public int levelOfDetail; // Detay seviyesi. Yüksek değerler daha az poligonlu, daha performanslı bir mesh oluşturur.
    public bool autoGenerateOnStart = true; // Oyun başladığında veya Inspector'da değer değiştiğinde haritayı otomatik üretir.

    [Header("Terrain Coloring")] // Arazi renklendirme ayarları
    public TerrainType[] regions; // Farklı yükseklikler için tanımlanmış arazi tipleri (su, kumsal, çimen vs.).

    // --- Private Değişkenler ---
    private MapDisplay display; // Üretilen haritayı ekranda göstermek için referans.
    private const int MAX_ISLAND_PLACEMENT_ATTEMPTS = 50; // Bir adayı yerleştirmek için maksimum deneme sayısı.

    // Genellikle data hazırlığı ve referans atamaları Awake'te yapılır.
    // Diğer scriptlerin Start() metodundan önce çalışmasını garantiler.
    void Awake()
    {
        // Bu objenin üzerindeki MapDisplay component'ini bul ve ata.
        display = GetComponent<MapDisplay>();
    }

    // Oyun döngüsünün ilk frame'inde çalışır.
    void Start()
    {
        // Eğer otomatik üretim açıksa, dünyayı yarat.
        if (autoGenerateOnStart)
        {
            GenerateWorld();
        }
    }

    /// <summary>
    /// Ana dünya oluşturma metodu. Orkestra şefi gibi diğer metodları yönetir.
    /// </summary>
    public void GenerateWorld()
    {
        // 1. Adaların merkez pozisyonlarını belirle.
        List<Vector2> islandCenters = GenerateIslandPositions();

        // 2. Temel arazi şekilleri için bir gürültü (noise) haritası oluştur.
        float[,] heightMap = Noise.GenerateNoiseMap(worldSize, worldSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        // 3. Adaların kenarlarını yumuşatmak için bir falloff (azalma) haritası oluştur.
        float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(worldSize, islandCenters, islandRadius, mainIslandSizeMultiplier, islandFalloffPower);

        // 4. Gürültü haritasını falloff haritasıyla birleştirerek adaları oluştur.
        // Falloff map'i çıkartarak haritanın kenarlarını "alçaltıyoruz".
        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
            }
        }

        // 5. Yükseklik haritasına göre bir renk haritası oluştur.
        Color[] colorMap = GenerateColorMap(heightMap);
        // 6. Yükseklik haritasından 3D bir arazi modeli (mesh) oluştur.
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        // 7. Renk haritasından bir doku (texture) oluştur.
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, worldSize, worldSize);

        // 8. Son olarak, üretilen mesh ve texture'ı ekranda göster.
        display.DrawMesh(meshData, texture);
    }

    /// <summary>
    /// Ana ada ve uydu adaların pozisyonlarını belirler.
    /// </summary>
    private List<Vector2> GenerateIslandPositions()
    {
        // 'seed' kullanarak rastgele ama tekrarlanabilir pozisyonlar üretiyoruz.
        System.Random prng = new System.Random(seed);
        List<Vector2> islandCenters = new List<Vector2>();

        // Ana ada her zaman haritanın tam ortasında.
        Vector2 mainIslandCenter = new Vector2(worldSize / 2f, worldSize / 2f);
        islandCenters.Add(mainIslandCenter);

        // İstenen sayıda uydu ada yerleştirmeye çalış.
        for (int i = 1; i < numberOfIslands; i++)
        {
            bool placedSuccessfully = false;
            // Bir pozisyon bulana kadar veya deneme hakkı bitene kadar döngüye gir.
            for (int attempt = 0; attempt < MAX_ISLAND_PLACEMENT_ATTEMPTS; attempt++)
            {
                // Rastgele bir açı ve mesafe belirle.
                float angle = (float)prng.NextDouble() * 360f;
                float distance = (float)(prng.NextDouble() * (maxSpawnRadius - minSpawnRadius) + minSpawnRadius);

                // Açı ve mesafeden yeni bir pozisyon (x, y) hesapla.
                float x = mainIslandCenter.x + distance * Mathf.Cos(angle * Mathf.Deg2Rad);
                float y = mainIslandCenter.y + distance * Mathf.Sin(angle * Mathf.Deg2Rad);
                Vector2 candidateCenter = new Vector2(x, y);

                // Bu pozisyon diğer adalara çok yakın mı diye kontrol et.
                bool isValid = true;
                foreach (Vector2 existingCenter in islandCenters)
                {
                    if (Vector2.Distance(candidateCenter, existingCenter) < minIslandDistance)
                    {
                        isValid = false; // Çok yakınsa geçersiz say.
                        break;
                    }
                }

                // Eğer pozisyon uygunsa, listeye ekle ve döngüden çık.
                if (isValid)
                {
                    islandCenters.Add(candidateCenter);
                    placedSuccessfully = true;
                    break;
                }
            }
            // Eğer tüm denemelere rağmen yerleştirilemediyse, uyarı ver.
            if (!placedSuccessfully)
            {
                Debug.LogWarning($"Bir uydu ada yerleştirilemedi. 'Min Island Distance' ({minIslandDistance}) veya spawn radius ayarlarını kontrol et.");
            }
        }
        return islandCenters;
    }

    /// <summary>
    /// Yükseklik haritasına ve bölgelere göre renk haritasını oluşturur.
    /// </summary>
    private Color[] GenerateColorMap(float[,] heightMap)
    {
        // Haritadaki her piksel için bir renk tutacak dizi.
        Color[] colorMap = new Color[worldSize * worldSize];
        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                // Mevcut pikselin yüksekliğini al.
                float currentHeight = heightMap[x, y];
                // Su seviyesinden başla, doğru bölgeyi bulana kadar yukarı çık.
                for (int i = 0; i < regions.Length; i++)
                {
                    // Eğer yükseklik, bölgenin max yüksekliğinden küçük veya eşitse...
                    if (currentHeight <= regions[i].height)
                    {
                        // O bölgenin rengini ata ve bu piksel için aramayı bitir.
                        colorMap[y * worldSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }

    // Inspector'da arazi tiplerini kolayca tanımlamak için kullanılan bir veri yapısı.
    [System.Serializable]
    public struct TerrainType
    {
        public string name; // Bölgenin adı (ör: "Kumsal")
        [Range(0, 1)]
        public float height; // Bu bölgenin bittiği maksimum yükseklik.
        public Color color;  // Bölgenin rengi.
    }
}