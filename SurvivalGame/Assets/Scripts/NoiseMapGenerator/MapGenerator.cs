using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSize = 513;
    public float waterLevel = 0.35f;

    [Header("Island Generation")]
    public int numberOfIslands = 5;
    public float islandRadius = 100f;
    public float mainIslandSizeMultiplier = 2f;
    public float islandFalloffPower = 2.5f;
    [Tooltip("Uyduların ana adadan en az ne kadar uzakta olacağı.")]
    public float minSpawnRadius = 150f;
    [Tooltip("Uyduların ana adadan en fazla ne kadar uzakta olacağı.")]
    public float maxSpawnRadius = 220f;
    [Tooltip("Adaların birbirine ne kadar yakın olabileceği.")]
    public float minIslandDistance = 100f;

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
    public MapDisplay display;
    public bool autoGenerateOnStart = true;
    public bool autoUpdate = false;

    [Header("Terrain Coloring")]
    public TerrainType[] regions;

    void Start()
    {
        if (autoGenerateOnStart)
        {
            GenerateWorld();
        }
    }

    public void GenerateWorld()
    {
        System.Random prng = new System.Random(seed);
        List<Vector2> islandCenters = new List<Vector2>();

        // 1. Ana Adayı Merkeze Yerleştir
        Vector2 mainIslandCenter = new Vector2(worldSize / 2f, worldSize / 2f);
        islandCenters.Add(mainIslandCenter);

        // 2. Diğer Adaları Ana Adanın Etrafına Yerleştir
        int maxAttemptsPerIsland = 50;

        for (int i = 1; i < numberOfIslands; i++) // 1'den başla çünkü 0 ana ada
        {
            bool placedSuccessfully = false;
            for (int attempt = 0; attempt < maxAttemptsPerIsland; attempt++)
            {
                // Rastgele bir açı ve mesafe belirle
                float angle = (float)prng.NextDouble() * 360f;
                float distance = (float)(prng.NextDouble() * (maxSpawnRadius - minSpawnRadius) + minSpawnRadius);

                // Pozisyonu hesapla
                float x = mainIslandCenter.x + distance * Mathf.Cos(angle * Mathf.Deg2Rad);
                float y = mainIslandCenter.y + distance * Mathf.Sin(angle * Mathf.Deg2Rad);
                Vector2 candidateCenter = new Vector2(x, y);

                // Diğer adalara çok yakın mı diye kontrol et
                bool isValid = true;
                foreach (Vector2 existingCenter in islandCenters)
                {
                    if (Vector2.Distance(candidateCenter, existingCenter) < minIslandDistance)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    islandCenters.Add(candidateCenter);
                    placedSuccessfully = true;
                    break;
                }
            }
            if (!placedSuccessfully)
            {
                Debug.LogWarning("Bir uydu ada yerleştirilemedi. 'Min Island Distance' veya spawn radius ayarlarını kontrol et.");
            }
        }

        // --- KODUN GERİ KALANI AYNI ---
        float[,] heightMap = Noise.GenerateNoiseMap(worldSize, worldSize, seed, noiseScale, octaves, persistence, lacunarity, offset);
        float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(worldSize, islandCenters, islandRadius, mainIslandSizeMultiplier, islandFalloffPower);

        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - falloffMap[x, y]);
            }
        }

        // ... (renklendirme ve mesh oluşturma kodları aynı kalıyor)
        Color[] colourMap = new Color[worldSize * worldSize];
        for (int y = 0; y < worldSize; y++)
        {
            for (int x = 0; x < worldSize; x++)
            {
                float currentHeight = heightMap[x, y];
                if (currentHeight > waterLevel)
                {
                    for (int i = 1; i < regions.Length; i++)
                    {
                        if (currentHeight <= regions[i].height)
                        {
                            colourMap[y * worldSize + x] = regions[i].color;
                            break;
                        }
                    }
                }
                else
                {
                    colourMap[y * worldSize + x] = regions[0].color;
                }
            }
        }

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, 2);
        Texture2D texture = TextureGenerator.TextureFromColorMap(colourMap, worldSize, worldSize);
        display.DrawMesh(meshData, texture);
    }


    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }
}