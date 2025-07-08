using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh };

    const int mapChunkSize = 241;
    [Range(0, 6)]
    public int levelOfDetail;

    [Header("Display Settings")]
    [Tooltip("Controls what to draw on the display plane.")]
    public DrawMode drawMode;

    [Tooltip("Reference to the MapDisplay script in the scene.")]
    [SerializeField] private MapDisplay display;

    [Header("Noise Settings")]
    [Tooltip("Scale of the noise. Smaller values zoom in, larger values zoom out.")]
    public float noiseScale;
    [Tooltip("Number of noise layers to stack. More octaves add more detail but cost more performance.")]
    public int octaves;
    [Tooltip("Controls how much smaller octaves influence the final shape. (0-1)")]
    [Range(0, 1)]
    public float persistence;
    [Tooltip("Controls the frequency increase of smaller octaves.")]
    public float lacunarity;
    [Tooltip("The seed for the random number generator to get different maps.")]
    public int seed;
    [Tooltip("Allows you to scroll through the noise map.")]
    public Vector2 offset;

    [Header("Mesh Settings")]
    [Tooltip("Overall height multiplier for the terrain mesh.")]
    public float meshHeightMultiplier;
    [Tooltip("Curve to control the height distribution of the terrain.")]
    public AnimationCurve meshHeightCurve;

    [Header("Terrain Coloring")]
    [Tooltip("Defines different regions of the terrain based on height.")]
    public TerrainType[] regions;

    [Header("Live Update")]
    [Tooltip("Automatically regenerates the map when a value is changed in the inspector.")]
    public bool autoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        if (drawMode == DrawMode.NoiseMap) display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColorMap) display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh) display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail),
            TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
    }

    private void OnValidate()
    {
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (persistence < 0)
        {
            persistence = 0;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;


}