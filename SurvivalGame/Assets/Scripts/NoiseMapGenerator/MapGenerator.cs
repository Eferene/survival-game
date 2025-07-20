using UnityEngine;

[RequireComponent(typeof(MapDisplay))]
public class MapGenerator : MonoBehaviour
{
    // Bu event, ada oluşturulduğunda tetiklenir.
    public static event System.Action OnIslandGenerated;

    // --- INSPECTOR AYARLARI ---
    [Header("Island Shape")]
    [Tooltip("Oluşturulacak adanın çözünürlüğü. LOD ile uyumlu olması için (2^n + 1) formatında olmalı. Örn: 129, 257, 513")]
    public int mapSize = 257;
    [Tooltip("Adanın merkezden kenarlara olan yaklaşık yarıçapı.")]
    public float islandRadius = 100f;
    [Tooltip("Adanın kenarlarının ne kadar keskin veya yumuşak olacağı.")]
    public float islandFalloffPower = 2.5f;

    [Header("Noise Settings")]
    int seed;
    public float noiseScale = 50f;
    public int octaves = 6;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;

    [Header("Mesh & Display")]
    public float meshHeightMultiplier = 25f;
    public AnimationCurve meshHeightCurve;
    [Range(0, 6)]
    public int levelOfDetail;
    public bool autoGenerateOnStart = true;

    [Header("Terrain Coloring")]
    [Tooltip("Daha pürüzsüz renk geçişleri için doku çözünürlüğünü artırır. 1 = harita boyutu, 4 = 4 kat daha yüksek çözünürlük.")]
    [Range(1, 8)]
    public int textureResolutionMultiplier = 4;
    public TerrainType[] regions;

    // --- Private Değişkenler ---
    [SerializeField] private MapDisplay display;

    [System.Serializable]
    public struct TerrainType { public string name; [Range(0, 1)] public float height; public Color color; }

    void Start()
    {
        // Script'i attığın objenin bir MapDisplay component'i olduğundan emin ol.
        if (display == null)
            display = GetComponent<MapDisplay>();
        if (autoGenerateOnStart)
            GenerateIsland();
    }

    public void GenerateIsland()
    {
        seed = Random.Range(0, 100000); // Her seferinde farklı bir seed kullanarak rastgelelik sağla.
        Debug.Log($"Yeni ada oluşturuluyor. Seed: {seed}");

        // 1. Yükseklik ve Falloff haritalarını oluştur.
        float[,] heightMap = Noise.GenerateNoiseMap(mapSize, mapSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        float[,] falloffMap = FalloffGenerator.GenerateSingleIslandFalloff(mapSize, islandRadius, islandFalloffPower);

        // 2. Haritaları birleştirerek adanın son şeklini oluştur.
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                // Falloff haritasını çıkararak kenarları okyanus seviyesine indir.
                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
            }
        }

        // 3. Renk haritası, mesh ve texture'ı oluştur.
        Color[] colorMap = GenerateColorMap(heightMap);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);

        // Doku oluşturmak için daha yüksek çözünürlük boyutlarını kullan.
        int textureSize = mapSize * textureResolutionMultiplier;
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, textureSize, textureSize);

        // 4. Oluşturulan mesh'i bu GameObject üzerine çizdir.
        display.DrawMesh(meshData, texture);

        // 5. Ada oluşturulduktan sonra event'i tetikle.
        OnIslandGenerated?.Invoke();
    }

    // Bu fonksiyon artık pürüzsüz sınırlar için yükseklik haritasını daha yüksek çözünürlükte yeniden örnekler.
    private Color[] GenerateColorMap(float[,] heightMap)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);

        // Doku için daha yüksek çözünürlüğü hesapla.
        int texWidth = mapWidth * textureResolutionMultiplier;
        int texHeight = mapHeight * textureResolutionMultiplier;

        Color[] colorMap = new Color[texWidth * texHeight];
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                // Düşük çözünürlüklü yükseklik haritasındaki karşılık gelen normalleştirilmiş konumu (0-1 aralığında) hesapla.
                float u = (float)x / (texWidth - 1);
                float v = (float)y / (texHeight - 1);

                // Bu konumdaki enterpolasyonlu (pürüzsüzleştirilmiş) yükseklik değerini al.
                float currentHeight = GetBilinearInterpolatedHeight(heightMap, u, v);

                // Renk bölgeleri için sert eşikleme (hard thresholding) uygula.
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * texWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }

    // Verilen bir yükseklik haritasındaki bir noktadaki değeri komşu dört noktayı kullanarak çift doğrusal
    // enterpolasyon ile hesaplar. Bu, pürüzsüz bir sonuç verir.
    private float GetBilinearInterpolatedHeight(float[,] heightMap, float u, float v)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);

        // Gerçek koordinatları (indeksleri) hesapla.
        float x = u * (mapWidth - 1);
        float y = v * (mapHeight - 1);

        // Enterpolasyon için komşu tam sayı indekslerini ve kesirli kısmı bul.
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(x0 + 1, mapWidth - 1);
        int y1 = Mathf.Min(y0 + 1, mapHeight - 1);

        float tx = x - x0;
        float ty = y - y0;

        // Komşu dört noktanın yükseklik değerlerini al.
        float h00 = heightMap[x0, y0];
        float h10 = heightMap[x1, y0];
        float h01 = heightMap[x0, y1];
        float h11 = heightMap[x1, y1];

        // Önce x ekseninde iki kez doğrusal enterpolasyon (Lerp) yap.
        float a = Mathf.Lerp(h00, h10, tx);
        float b = Mathf.Lerp(h01, h11, tx);

        // Son olarak, önceki iki sonucun arasında y ekseninde enterpolasyon yap.
        return Mathf.Lerp(a, b, ty);
    }
}
