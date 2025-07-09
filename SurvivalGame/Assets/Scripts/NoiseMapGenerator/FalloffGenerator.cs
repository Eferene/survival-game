using UnityEngine;
using System.Collections.Generic;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int size, List<Vector2> islandCenters, float islandRadius, float mainIslandMultiplier, float falloffPower)
    {
        float[,] map = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float closestIslandDist = float.MaxValue;
                int closestIslandIndex = 0; // En yakın adanın index'ini tut

                // Her piksel için en yakın ada merkezini ve index'ini bul
                for (int i = 0; i < islandCenters.Count; i++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), islandCenters[i]);
                    if (dist < closestIslandDist)
                    {
                        closestIslandDist = dist;
                        closestIslandIndex = i;
                    }
                }

                // En yakın adanın index'ine göre büyüklüğü belirle
                // index 0 = ana ada
                float currentIslandRadius = (closestIslandIndex == 0) ? islandRadius * mainIslandMultiplier : islandRadius;

                // Ada kenarından uzaklığa göre falloff değeri hesapla
                float value = closestIslandDist / currentIslandRadius;
                value = Mathf.Pow(value, falloffPower);

                // Değeri 0-1 arasında tut
                map[x, y] = Mathf.Clamp01(value);
            }
        }
        return map;
    }
}