using UnityEngine;

// Bu statik sınıf, Perlin Noise kullanarak prosedürel arazi yükseklik haritaları oluşturur.
// The secret sauce for natural-looking terrain.
public static class Noise
{
    // Gürültü haritası üreten ana fonksiyon.
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Seed'e dayalı rastgele sayı üreteci. Bu, oktav offset'lerinin her seferinde aynı olmasını sağlar.
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        // Her oktav için rastgele bir offset oluştur. Bu, katmanların simetrik olmasını engeller.
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Sıfıra bölme hatasını engellemek için scale'e minimum bir değer ver.
        if (scale <= 0) scale = 0.0001f;

        // --- Normalizasyon için ön hesaplama ---
        float maxPossibleHeight = 0;
        float amplitude = 1;
        // Tüm oktavların genliklerini toplayarak teorik maksimum yüksekliği bul.
        for (int i = 0; i < octaves; i++)
        {
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        // Haritanın merkezden hesaplanması için yarım genişlik/yükseklik değerleri.
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // MİKRO-OPTİMİZASYON: Döngü içinde sürekli bölme yapmak yerine, 
        // bir kere tersini alıp onunla çarpıyoruz. It's a small thing, but it adds up.
        float scaleInverse = 1f / scale;

        // Haritanın her pikseli için döngüye gir.
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // Her piksel için oktav hesaplamalarını sıfırla.
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                // --- Oktavları birleştirme ---
                for (int i = 0; i < octaves; i++)
                {
                    // Perlin noise için örnekleme koordinatlarını hesapla.
                    float sampleX = (x - halfWidth) * scaleInverse * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) * scaleInverse * frequency + octaveOffsets[i].y;

                    // Unity'nin PerlinNoise'ı 0-1 arası değer verir. Biz -1 ile 1 arası istiyoruz.
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // Gürültü değerini mevcut genlik ile çarpıp toplam yüksekliğe ekle.
                    noiseHeight += perlinValue * amplitude;

                    // Bir sonraki oktav için genliği (etki) ve frekansı (detay) güncelle.
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Hesaplanan gürültü değerini haritaya ata.
                noiseMap[x, y] = noiseHeight;
            }
        }

        // --- Normalizasyon ---
        // Tüm gürültü değerlerini 0-1 arasına çek.
        // Bu, ayarlar ne olursa olsun, sonucun her zaman tutarlı bir aralıkta kalmasını sağlar.
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // Önceden hesapladığımız teorik max yüksekliğe bölerek normalize ediyoruz.
                // 1.75f is a "magic number" that just makes the terrain look better.
                float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.75f);
                noiseMap[x, y] = Mathf.Clamp01(normalizedHeight);
            }
        }

        return noiseMap;
    }
}