using UnityEngine;
using System.Collections;

/// <summary>
/// Harita oluşturulduktan sonra oyuncuyu güvenli bir noktaya yerleştirmekten sorumludur.
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Haritayı oluşturan ana script. Nereye spawn olacağımızı bilmek için bu şart.")]
    [SerializeField] private MapGenerator mapGenerator;
    [Tooltip("Pozisyonu ayarlanacak, sahnede zaten var olan karakter objesi.")]
    public GameObject playerCharacter;

    [Header("Spawn Ayarları")]
    [Tooltip("Karakterin spawn olabileceği minimum 'normalize' yükseklik. 0 su, 1 en yüksek tepe.")]
    [Range(0f, 1f)]
    [SerializeField] private float minSpawnNormalizedHeight = 0.3f;
    [Tooltip("Güvenli yer ararken merkezden dışa doğru spiral çizerken adımlar arasındaki mesafe.")]
    [SerializeField] private float searchStep = 2f;
    [Tooltip("Sonsuz döngüyü önlemek için spiral aramada denenecek maksimum adım sayısı.")]
    [SerializeField] private int maxSearchRadiusSteps = 100;

    private MeshCollider meshCollider;
    private Rigidbody playerRB;

    private void Start()
    {
        playerRB = playerCharacter.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Bu metod, MapGenerator tarafından harita hazır olduğunda tetiklenir.
    /// </summary>
    public void OnMapReady()
    {
        if (playerCharacter == null || mapGenerator == null)
        {
            Debug.LogError("PlayerSpawnManager'da Player Character veya MapGenerator referansı eksik!");
            return;
        }

        // Haritanın collider'ını al.
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
            Debug.Log($"Karakter için güvenli spawn noktası bulundu: {spawnPoint}");
            playerRB.isKinematic = true; // Taşıma sırasında fizik etkilerini kapat.
            yield return new WaitForFixedUpdate(); // Fizik güncellemesi için bir frame bekle.
            playerCharacter.transform.position = spawnPoint + Vector3.up * 5f;
            playerRB.isKinematic = false; // Fizik etkilerini tekrar aç.


        }
        else
        {
            // Uygun bir yer bulunamadıysa hata ver.
            Debug.LogError("Haritada spawn için uygun bir yer bulunamadı.");
            playerCharacter.transform.position = new Vector3(0f, 50f, 0f);
        }
    }

    /// <summary>
    /// Haritanın merkezinden başlayarak spiral şeklinde dışa doğru arama yaparak,
    /// belirlenen minimum yüksekliğin üzerinde güvenli bir nokta bulmaya çalışır.
    /// </summary>
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
                    safePoint = hit.point;
                    return true;
                }
            }

            // Bu kısım, (x, z) koordinatlarının spiral çizmesini sağlayan matematik.
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