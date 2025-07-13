using UnityEngine;
using System.Collections.Generic;

// Bu statik sınıf, arazi haritasının kenarlarını ve adaların çevresini
// yumuşatarak okyanus/deniz efekti yaratan bir "falloff" haritası üretir.
public static class FalloffGenerator
{
    /// <summary>
    /// Performans için optimize edilmiş falloff haritası oluşturur.
    /// Her piksel için tüm adaları kontrol etmek yerine, dünyayı bir grid'e böler
    /// ve her piksel için sadece o grid hücresindeki en yakın aday adaları kontrol eder.
    /// This is some next-level optimization, my man.
    /// </summary>
    public static float[,] GenerateFalloffMap(int size, List<Vector2> islandCenters, float islandRadius, float mainIslandMultiplier, float falloffPower)
    {
        float[,] map = new float[size, size];

        // --- OPTİMİZASYON: Grid Sistemi ---
        // Dünyayı, içine düşebilecek potansiyel adaları tutan hücrelere böl.
        int cellSize = Mathf.CeilToInt(islandRadius * mainIslandMultiplier) + 1; // Hücre boyutu en büyük adadan biraz büyük olmalı.
        int numCells = Mathf.CeilToInt((float)size / cellSize);
        List<int>[,] cellIslandIndices = new List<int>[numCells, numCells];

        // 1. ÖN HESAPLAMA (Pre-computation): Her hücre için potansiyel en yakın adaları bul.
        // Bu, her pikselde tüm adaları gezmekten kat kat daha hızlıdır.
        for (int cy = 0; cy < numCells; cy++)
        {
            for (int cx = 0; cx < numCells; cx++)
            {
                Vector2 cellCenter = new Vector2((cx + 0.5f) * cellSize, (cy + 0.5f) * cellSize);
                cellIslandIndices[cx, cy] = new List<int>();

                // Bu hücreye "potansiyel olarak en yakın" olabilecek tüm adaları bul.
                float maxDist = (Mathf.Sqrt(2) * cellSize) / 2f + islandRadius * mainIslandMultiplier;

                for (int i = 0; i < islandCenters.Count; i++)
                {
                    // Eğer bir adanın merkezi, hücrenin merkezine belli bir mesafeden yakınsa,
                    // o adayı bu hücrenin "adaylar" listesine ekle.
                    if (Vector2.Distance(islandCenters[i], cellCenter) < maxDist)
                    {
                        cellIslandIndices[cx, cy].Add(i);
                    }
                }
            }
        }

        // 2. PİKSEL DEĞERLERİNİ HESAPLAMA: Optimize edilmiş listeyi kullanarak haritayı doldur.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 currentPosition = new Vector2(x, y);
                float minDistance = float.MaxValue;
                int closestIslandIndex = 0;

                // Pikselin hangi hücrede olduğunu bul.
                int cellX = Mathf.Clamp(x / cellSize, 0, numCells - 1);
                int cellY = Mathf.Clamp(y / cellSize, 0, numCells - 1);

                // Sadece o hücrenin aday adalarını kontrol et. Much faster!
                foreach (int islandIndex in cellIslandIndices[cellX, cellY])
                {
                    float dist = Vector2.Distance(currentPosition, islandCenters[islandIndex]);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestIslandIndex = islandIndex;
                    }
                }

                // En yakın adanın ana ada olup olmadığını kontrol ederek doğru yarıçapı kullan.
                float currentIslandRadius = (closestIslandIndex == 0) ? islandRadius * mainIslandMultiplier : islandRadius;
                // Değeri hesapla, gücünü al ve 0-1 arasına sıkıştır.
                float value = minDistance / currentIslandRadius;
                value = Mathf.Pow(value, falloffPower);
                map[x, y] = Mathf.Clamp01(value);
            }
        }
        return map;
    }
}