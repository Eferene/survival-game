using UnityEngine;

// Bu statik sınıf, renk veya yükseklik haritalarından Texture2D nesneleri oluşturur.
public static class TextureGenerator
{
    // Bir renk dizisinden (Color[]) 2D bir texture oluşturur.
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        // filterMode.Point, pikseller arası geçişi yumuşatmaz, keskin bırakır.
        // Bu, bloklu, bölgeli haritalar için daha iyi bir görünüm sağlar.
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 9; // Anisotropic filtering'i kapatır, bu da performansı artırır.

        // wrapMode.Clamp, texture'ın kenarlarının tekrar etmesini (tile) engeller.
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply(); // Değişiklikleri texture'a uygula.
        return texture;
    }

    // Bir yükseklik haritasından (float[,]) siyah-beyaz (grayscale) bir texture oluşturur.
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Yükseklik değerini (0-1 arası) siyah (0) ve beyaz (1) arasında bir gri tonuna çevirir.
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }
        // Renk haritasını texture'a çeviren diğer fonksiyonu çağır.
        return TextureFromColorMap(colorMap, width, height);
    }
}