using UnityEngine;

[RequireComponent(typeof(MapDisplay))]
public class MapGenerator : MonoBehaviour
{
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

    // YENİDEN DÜZENLENDİ: Bu fonksiyon artık sadece tek bir ada oluşturur.
    public void GenerateIsland()
    {
        seed = Random.Range(0, 100000); // Her seferinde farklı bir seed kullanarak rastgelelik sağla.
        Debug.Log($"Yeni ada oluşturuluyor. Seed: {seed}");
        // 1. Yükseklik ve Falloff haritalarını oluştur.
        float[,] heightMap = Noise.GenerateNoiseMap(mapSize, mapSize, seed, noiseScale, octaves, persistence, lacunarity, offset);

        // Falloff haritası artık her zaman haritanın merkezi için oluşturulur.
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
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, mapSize, mapSize);

        // 4. Oluşturulan mesh'i bu GameObject üzerine çizdir.
        display.DrawMesh(meshData, texture);
    }

    private Color[] GenerateColorMap(float[,] heightMap)
    {
        Color[] colorMap = new Color[mapSize * mapSize];
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float currentHeight = heightMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }
}