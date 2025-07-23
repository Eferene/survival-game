using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bu class, Inspector'da ayarlanabilen yerleştirilebilir bir obje türünü tanımlar.
/// Örneğin bir "Ağaç" veya "Kaya" türü oluşturup özelliklerini belirleyebilirsin.
/// </summary>
[System.Serializable]
public class PlaceableObject
{
    [Tooltip("Inspector'da bu obje türünü tanımak için kullanılacak isim.")]
    public string name;
    [Tooltip("Yerleştirilecek olan asıl obje prefab'ı.")]
    public GameObject prefab;
    [Tooltip("Bu objenin spawn olabileceği minimum arazi yüksekliği.")]
    [Range(0f, 1f)] public float minHeight = 0f;
    [Tooltip("Bu objenin spawn olabileceği maksimum arazi yüksekliği.")]
    [Range(0f, 1f)] public float maxHeight = 1f;
    [Tooltip("Bu objenin spawn olma olasılığı/yoğunluğu. Diğer objelerle rekabet eder.")]
    [Range(0f, 100f)] public float density = 10f;
    [Tooltip("Prefab'ı yerleştirdikten sonra Y ekseninde ne kadar yukarı/aşağı kaydırılacağı.")]
    public float yOffset = 0f;
    [Tooltip("İşaretliyse, obje bulunduğu zeminin eğimine göre hizalanır.")]
    public bool alignToSurface = false;
    [Tooltip("İşaretliyse, objenin Y eksenindeki rotasyonu rastgele ayarlanır.")]
    public bool randomRotationY = true;
}

// Bu component'in olduğu objeye otomatik olarak bir MeshCollider eklenmesini zorunlu kılar.
[RequireComponent(typeof(MeshCollider))]
public class ProceduralObjectPlacer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Objeleri yerleştirmek için harita verisini sağlayacak olan MapGenerator. This is mission-critical, bro.")]
    public MapGenerator mapGenerator;

    [Header("Placement Settings")]
    [Tooltip("Prosedürel olarak yerleştirilecek obje türlerinin listesi.")]
    [SerializeField] private PlaceableObject[] objectsToPlace;
    [Tooltip("Objelerin yerleştirileceği grid'in adım aralığı. Düşük değerler daha yoğun yerleşim demektir.")]
    [Range(5f, 50f)]
    public float placementStep = 10f;

    [Header("Hierarchy Settings")]
    [Tooltip("Oluşturulan tüm objelerin bu parent obje altına toplanmasını sağlar.")]
    [SerializeField] private GameObject environment;

    // MapGenerator'dan alınan yükseklik haritası.
    private float[,] heightMap;
    // Oluşturulan objeleri hiyerarşide düzenli tutmak için kullanılan parent transform.
    private Transform objectParentContainer;

    /// <summary>
    /// Bu metod MapGenerator tarafından çağrılır, haritanın hazır olduğunu bildirir.
    /// </summary>
    public void HandleMapGeneration(MapData mapData)
    {
        // Gelen harita verisini kendi değişkenimize atıyoruz.
        this.heightMap = mapData.heightMap;

        if (Application.isPlaying)
        {
            // Collider'ın güncellenmesini beklemek için bir sonraki frame'de objeleri yerleştirecek Coroutine'i başlat.
            StartCoroutine(PlaceObjectsAfterFrame());
        }
        else // Eğer Editör'de isek...
        {
            // Editör donmasın diye bir sonraki update döngüsünde çalışacak şekilde ayarla.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += PlaceObjects;
#endif
        }
    }

    // Oyun modunda bir frame bekleyip objeleri yerleştiren Coroutine.
    private IEnumerator PlaceObjectsAfterFrame()
    {
        // Bir sonraki frame'e kadar bekle.
        yield return null;
        // Şimdi objeleri yerleştir.
        PlaceObjects();
    }

    /// <summary>
    /// Mevcut harita verisine göre objeleri yerleştirir.
    /// </summary>
    public void PlaceObjects()
    {
        // Başlamadan önce, önceden yerleştirilmiş tüm objeleri temizle.
        ClearObjects();

        // Eğer MapGenerator referansı atanmamışsa, harita verisini ondan isteyemeyiz.
        if (mapGenerator != null)
        {
            // En güncel harita verisini MapGenerator'dan al.
            MapData? data = mapGenerator.GetLastGeneratedMapData();
            if (data.HasValue)
            {
                this.heightMap = data.Value.heightMap;
            }
        }

        // Eğer heightMap hala boşsa, hata ver ve işlemi durdur. Can't do magic without the data, kral.
        if (this.heightMap == null)
        {
            Debug.LogError("HeightMap alınamadı! Önce haritayı oluşturun ve 'Map Generator' referansını atayın.");
            return;
        }

        // Gerekli olan MeshCollider'ı al. Işın göndermek için bu lazım.
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            Debug.LogWarning("Obje yerleştirme atlandı çünkü MeshCollider veya mesh hazır değil. Önce harita oluşturun.");
            return;
        }

        // Haritanın sınırlarını (bounds) al. Bu sınırlar içinde dolaşacağız.
        Bounds bounds = meshCollider.bounds;

        // Hiyerarşide düzen için ana bir parent obje oluştur.
        GameObject mainParent = new GameObject("Procedurally Placed Objects " + gameObject.name);
        mainParent.transform.SetParent(environment.transform);
        objectParentContainer = mainParent.transform;

        // Her obje türü için ayrı bir parent obje oluştur (örn: "Ağaçlar", "Kayalar").
        Dictionary<PlaceableObject, Transform> parentTransforms = new Dictionary<PlaceableObject, Transform>();
        foreach (PlaceableObject objType in objectsToPlace)
        {
            GameObject typeParent = new GameObject(objType.name + "s");
            typeParent.transform.SetParent(objectParentContainer);
            parentTransforms.Add(objType, typeParent.transform);
        }

        // Haritanın x ve z eksenlerinde, belirlenen 'placementStep' aralıklarıyla dolaş.
        for (float x = bounds.min.x; x < bounds.max.x; x += placementStep)
        {
            for (float z = bounds.min.z; z < bounds.max.z; z += placementStep)
            {
                // Yerleşimin grid gibi görünmemesi için pozisyona küçük bir rastgelelik ekle.
                float sampleX = x + Random.Range(-placementStep / 2f, placementStep / 2f);
                float sampleZ = z + Random.Range(-placementStep / 2f, placementStep / 2f);
                Vector3 rayStart = new Vector3(sampleX, bounds.max.y + 10f, sampleZ);

                // Bu noktadan aşağı doğru bir ışın (Raycast) göndererek zemini bul.
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, bounds.size.y + 20f, 1, QueryTriggerInteraction.Ignore) && hit.collider == meshCollider)
                {
                    // Işının çarptığı yerdeki normalize edilmiş yüksekliği (0-1 arası) al.
                    float normalizedHeight = MapGenerator.GetBilinearInterpolatedHeight(this.heightMap, hit.textureCoord.x, hit.textureCoord.y);

                    // Bu yükseklikte spawn olabilecek obje türlerini bul.
                    List<PlaceableObject> candidates = new List<PlaceableObject>();
                    float totalDensity = 0f;
                    foreach (PlaceableObject objType in objectsToPlace)
                    {
                        if (normalizedHeight >= objType.minHeight && normalizedHeight <= objType.maxHeight)
                        {
                            candidates.Add(objType);
                            totalDensity += objType.density;
                        }
                    }

                    // Eğer aday varsa, yoğunluklarına göre birini rastgele seç.
                    if (candidates.Count > 0)
                    {
                        float randomWeight = Random.Range(0f, totalDensity);
                        PlaceableObject chosenObject = null;
                        foreach (PlaceableObject candidate in candidates)
                        {
                            if (randomWeight < candidate.density)
                            {
                                chosenObject = candidate;
                                break;
                            }
                            randomWeight -= candidate.density;
                        }

                        // Eğer bir obje seçildiyse ve prefab'ı varsa, onu oluştur (Instantiate).
                        if (chosenObject != null && chosenObject.prefab != null)
                        {
                            Vector3 position = hit.point + new Vector3(0, chosenObject.yOffset, 0);
                            Quaternion rotation = chosenObject.alignToSurface ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity;
                            if (chosenObject.randomRotationY)
                            {
                                rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
                            }
                            Instantiate(chosenObject.prefab, position, rotation, parentTransforms[chosenObject]);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Bu script tarafından daha önce oluşturulmuş tüm objeleri hiyerarşiden siler.
    /// </summary>
    public void ClearObjects()
    {
        string parentName = "Procedurally Placed Objects " + gameObject.name;

#if UNITY_EDITOR
        // Eğer oyunda değilsek (Editör'de isek) DestroyImmediate kullanmalıyız.
        if (!Application.isPlaying)
        {
            Transform existing = environment.transform.Find(parentName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
            return;
        }
#endif

        // Eğer oyundaysak normal Destroy yeterlidir.
        if (Application.isPlaying)
        {
            Transform existing = environment.transform.Find(parentName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
        }
    }
}