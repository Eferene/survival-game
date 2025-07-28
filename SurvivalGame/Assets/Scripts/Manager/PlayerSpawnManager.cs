using UnityEngine;
using System.Collections;

/// <summary>
/// Harita oluşturulduktan sonra oyuncuyu güvenli bir noktaya yerleştirmekten sorumludur.
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Haritayı oluşturan ana script. Nereye spawn olacağımızı bilmek için bu şart.")]
    public MapGenerator mapGenerator;
    [Tooltip("Pozisyonu ayarlanacak, sahnede zaten var olan karakter objesi.")]
    public GameObject playerCharacter;

    [Header("Spawn Ayarları")]
    [Tooltip("Karakterin spawn olabileceği minimum 'normalize' yükseklik. 0 su, 1 en yüksek tepe.")]
    [Range(0f, 1f)]
    public float minSpawnNormalizedHeight = 0.3f;
    [Tooltip("Güvenli yer ararken merkezden dışa doğru spiral çizerken adımlar arasındaki mesafe.")]
    public float searchStep = 2f;
    [Tooltip("Sonsuz döngüyü önlemek için spiral aramada denenecek maksimum adım sayısı.")]
    public int maxSearchRadiusSteps = 100;

    // --- Private Değişkenler ---
    // Haritanın fiziksel collider'ı. Işın göndermek için kullanılır.
    private MeshCollider meshCollider;

    /// <summary>
    /// Bu metod, MapGenerator tarafından harita hazır olduğunda tetiklenir.
    /// </summary>
    public void OnMapReady()
    {
        // Gerekli referanslar atanmış mı diye kontrol et. No references, no party.
        if (playerCharacter == null || mapGenerator == null)
        {
            Debug.LogError("PlayerSpawnManager'da Player Character veya MapGenerator referansı eksik!");
            return;
        }

        // Haritanın collider'ını al. Bu çok önemli, çünkü zemini bununla bulacağız.
        meshCollider = mapGenerator.GetComponentInChildren<MeshCollider>();
        if (meshCollider == null)
        {
            Debug.LogError("Haritada MeshCollider bulunamadı! Karakter pozisyonu ayarlanamıyor.");
            return;
        }

        // Karakteri bulup taşıyacak olan Coroutine'i başlat.
        StartCoroutine(FindAndMoveCharacter());
    }

    /// <summary>
    /// Güvenli bir spawn noktası bulup karakteri oraya taşıyan Coroutine.
    /// </summary>
    private IEnumerator FindAndMoveCharacter()
    {
        // Collider'ın tamamen güncellendiğinden emin olmak için bir frame beklemek her zaman daha güvenlidir.
        yield return null;

        // Güvenli bir spawn noktası bulmayı dene.
        if (TryFindSafeSpawnPoint(out Vector3 spawnPoint))
        {
            // Nokta bulunduysa, karakteri oraya ışınla.
            playerCharacter.transform.position = new Vector3(spawnPoint.x, spawnPoint.y + 5, spawnPoint.z);
            playerCharacter.transform.rotation = Quaternion.identity; // Rotasyonu sıfırlamak temiz bir başlangıç sağlar.
        }
        else
        {
            // Uygun bir yer bulunamadıysa hata ver. Muhtemelen bütün harita su altında kaldı.
            Debug.LogError("Haritada spawn için uygun bir yer bulunamadı.");
            playerCharacter.transform.position = new Vector3(0f, 50f, 0f);
        }
    }

    /// <summary>
    /// Haritanın merkezinden başlayarak spiral şeklinde dışa doğru arama yaparak,
    /// belirlenen minimum yüksekliğin üzerinde güvenli bir nokta bulmaya çalışır.
    /// </summary>
    /// <param name="safePoint">Eğer bulunursa, güvenli noktanın pozisyonunu döndürür.</param>
    /// <returns>Güvenli bir nokta bulunursa true, bulunamazsa false döner.</returns>
    private bool TryFindSafeSpawnPoint(out Vector3 safePoint)
    {
        // Minumum dünya yüksekliğini hesapla (normalize değeri gerçek yüksekliğe çevir).
        float normalizedHeightFromCurve = mapGenerator.meshHeightCurve.Evaluate(minSpawnNormalizedHeight);
        float minWorldHeight = normalizedHeightFromCurve * mapGenerator.meshHeightMultiplier;
        float x = 0, z = 0, dx = 0, dy = -1;
        int steps = 0;
        int maxSteps = maxSearchRadiusSteps * maxSearchRadiusSteps;

        // Maksimum deneme sayısına ulaşana kadar döngüye devam et.
        while (steps < maxSteps)
        {
            // Mevcut arama noktasından aşağı doğru bir ışın gönder.
            Vector3 currentPoint = new Vector3(x, meshCollider.bounds.max.y + 10f, z);
            if (Physics.Raycast(currentPoint, Vector3.down, out RaycastHit hit, 500f) && hit.collider == meshCollider)
            {
                // Eğer ışının çarptığı yerin yüksekliği minimum yükseklikten fazlaysa...
                if (hit.point.y > minWorldHeight)
                {
                    // bingo! Güvenli noktayı bulduk.
                    safePoint = hit.point;
                    return true; // We got a winner!
                }
            }

            // Bu kısım, (x, z) koordinatlarının spiral çizmesini sağlayan matematik. Don't touch if you don't know, dude.
            if ((x == z) || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
            {
                float temp = dx;
                dx = -dy;
                dy = temp;
            }
            x += dx * searchStep;
            z += dy * searchStep;
            steps++;
        }

        // Döngü bitti ve hala bir yer bulamadıysak, başarısız olduk.
        safePoint = Vector3.zero;
        return false;
    }
}