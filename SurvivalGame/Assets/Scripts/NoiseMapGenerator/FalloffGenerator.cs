using UnityEngine;

public static class FalloffGenerator
{
    // YENİ FONKSİYON: Sadece merkezde bulunan tek bir ada için dairesel falloff haritası oluşturur.
    public static float[,] GenerateSingleIslandFalloff(int size, float radius, float power)
    {
        float[,] map = new float[size, size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Merkeze olan uzaklığı hesapla.
                float distance = Vector2.Distance(new Vector2(x, y), center);
                // Uzaklığı yarıçapa bölerek 0-1 arası bir değere yaklaştır.
                float value = distance / radius;
                // Değerin gücünü alarak kenar yumuşaklığını ayarla.
                value = Mathf.Pow(value, power);
                // Değeri 0-1 arasına sıkıştır ve haritaya ata.
                map[x, y] = Mathf.Clamp01(value);
            }
        }
        return map;
    }
}