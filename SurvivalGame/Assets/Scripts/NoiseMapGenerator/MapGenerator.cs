using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Bu script'in çalışması için MapDisplay component'inin aynı GameObject'te bulunmasını zorunlu kılar.
[RequireComponent(typeof(MapDisplay))]
public class MapGenerator : MonoBehaviour
{
    // --- INSPECTOR AYARLARI ---

    [Header("World Settings")]
    public int worldSize = 513;

    [Header("Island Generation")]
    [Tooltip("Ana ada dahil toplam ada sayısı. Sabit açılar nedeniyle en fazla 5 olabilir (1 ana + 4 uydu).")]
    [Range(1, 5)]
    public int numberOfIslands = 5;
    public float islandRadius = 100f;
    public float mainIslandSizeMultiplier = 2f;
    public float islandFalloffPower = 2.5f;
    [Tooltip("Uydu adaların ana adanın merkezinden uzaklığı.")]
    public float satelliteDistance = 200f;

    [Header("Noise Settings")]
    public int seed;
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

    // --- Public Değişkenler ---
    public List<Island> generatedIslands { get; private set; }

    // --- Private Değişkenler ---
    [SerializeField] private MapDisplay display;

    // YENİ: Ada bilgilerini (ID ve merkez) bir arada tutacak yapı.
    [System.Serializable]
    public struct Island
    {
        public int id;
        public Vector2 center;
    }

    // Inspector'da arazi tiplerini kolayca tanımlamak için kullanılan bir veri yapısı.
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        [Range(0, 1)]
        public float height;
        public Color color;
    }

    void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateWorld();
        }
    }

    public void GenerateWorld()
    {
        generatedIslands = GenerateIslandPositions();
        List<Vector2> islandCenters = generatedIslands.Select(island => island.center).ToList();

        float[,] heightMap = Noise.GenerateNoiseMap(worldSize, worldSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(worldSize, islandCenters, islandRadius, mainIslandSizeMultiplier, islandFalloffPower);

        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
            }
        }

        Color[] colorMap = GenerateColorMap(heightMap);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, worldSize, worldSize);

        display.DrawMesh(meshData, texture);
    }

    /// <summary>
    /// Ana ada ve uydu adaların pozisyonlarını belirler. Artık sabit açılar ve sabit uzaklık kullanır.
    /// </summary>
    private List<Island> GenerateIslandPositions()
    {
        float[] satelliteAngles = { 0f, 90f, 180f, 270f };
        List<Island> islands = new List<Island>();

        // Ana ada (ID=0) her zaman haritanın tam ortasında.
        Vector2 mainIslandCenter = new Vector2(worldSize / 2f, worldSize / 2f);
        islands.Add(new Island { id = 0, center = mainIslandCenter });

        // İstenen sayıda uydu ada yerleştir.
        for (int i = 1; i < numberOfIslands; i++)
        {
            if (i - 1 >= satelliteAngles.Length)
            {
                Debug.LogWarning("En fazla 5 ada ekleyebilirsin.");
                break;
            }

            // Sabit açı ve Inspector'dan gelen sabit uzaklığı kullan.
            float angle = satelliteAngles[i - 1];

            float x = mainIslandCenter.x + satelliteDistance * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = mainIslandCenter.y + satelliteDistance * Mathf.Sin(angle * Mathf.Deg2Rad);
            Vector2 candidateCenter = new Vector2(x, y);

            // Direkt ekle, çünkü artık çakışma ihtimali yok.
            islands.Add(new Island { id = i, center = candidateCenter });
        }
        return islands;
    }

    private Color[] GenerateColorMap(float[,] heightMap)
    {
        Color[] colorMap = new Color[worldSize * worldSize];
        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                float currentHeight = heightMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * worldSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }
}
