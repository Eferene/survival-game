using UnityEngine;

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}

[RequireComponent(typeof(MapDisplay))]
public class MapGenerator : MonoBehaviour
{
    public static event System.Action<MapData> OnIslandGenerated;

    [Header("Island Shape")]
    public int mapSize = 257;
    public float islandRadius = 100f;
    public float islandFalloffPower = 2.5f;

    [Header("Noise Settings")]
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
    [Range(1, 8)]
    public int textureResolutionMultiplier = 4;
    public TerrainType[] regions;

    [SerializeField] private MapDisplay display;

    private MapData? lastGeneratedMapData;

    [System.Serializable]
    public struct TerrainType { public string name; [Range(0, 1)] public float height; public Color color; }

    void Start()
    {
        if (display == null) display = GetComponent<MapDisplay>();
        if (autoGenerateOnStart)
        {
            // DEĞİŞTİ: Oyun başladığında event'in tetiklenmesi için 'true' parametresiyle çağır.
            GenerateIsland(true);
        }
    }

    // DEĞİŞTİ: Metod artık bir bool parametre alıyor.
    public void GenerateIsland(bool notifyListeners = true)
    {
        int seed = Random.Range(0, 100000);
        // Debug.Log($"Yeni ada oluşturuluyor. Seed: {seed}");

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

        Color[] colorMap = GenerateColorMap(heightMap);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);

        int textureSize = mapSize * textureResolutionMultiplier;
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, textureSize, textureSize);

        display.DrawMesh(meshData, texture);

        // DEĞİŞTİ: Sadece istenirse event'i tetikle.
        if (notifyListeners && lastGeneratedMapData.HasValue)
        {
            OnIslandGenerated?.Invoke(lastGeneratedMapData.Value);
        }
    }

    public MapData? GetLastGeneratedMapData()
    {
        return lastGeneratedMapData;
    }

    private Color[] GenerateColorMap(float[,] heightMap)
    {
        int mapWidth = heightMap.GetLength(0);
        int mapHeight = heightMap.GetLength(1);
        int texWidth = mapWidth * textureResolutionMultiplier;
        int texHeight = mapHeight * textureResolutionMultiplier;
        Color[] colorMap = new Color[texWidth * texHeight];
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                float u = (float)x / (texWidth - 1);
                float v = (float)y / (texHeight - 1);
                float currentHeight = GetBilinearInterpolatedHeight(heightMap, u, v);
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