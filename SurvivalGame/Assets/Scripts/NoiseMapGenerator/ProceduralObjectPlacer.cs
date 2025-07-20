// Ana Script: ProceduralObjectPlacer.cs
// Bu scripti MeshFilter ve MeshCollider'a sahip olan harita objene eklemelisin.

using UnityEngine;
using System.Collections.Generic;

// Bu sınıf, Inspector'da her bir obje türü için ayarları tutar.
// [System.Serializable] sayesinde Inspector'da görünebilir hale gelir.
[System.Serializable]
public class PlaceableObject
{
    public string name; // Inspector'da kolayca tanımak için isim
    public GameObject prefab; // Yerleştirilecek objenin prefab'ı
    [Range(0f, 1f)]
    public float minHeight = 0f; // Yerleştirme için minimum yükseklik (normalize edilmiş, 0-1 arası)
    [Range(0f, 1f)]
    public float maxHeight = 1f; // Yerleştirme için maksimum yükseklik (normalize edilmiş, 0-1 arası)
    [Range(0f, 100f)]
    public float density = 10f; // Yoğunluk/Şans. Bu değer ne kadar yüksekse o kadar sık obje yerleşir.
    [Tooltip("Objeyi dikey eksende kaydırmak için kullanılır. Pivotu ortada olan modeller için pozitif değer girin.")]
    public float yOffset = 0f;
    public bool alignToSurface = false; // Obje yüzeyin eğimine göre mi hizalansın? (Taşlar için ideal)
    public bool randomRotationY = true; // Y ekseninde rastgele döndürülsün mü? (Ağaçlar için ideal)
}

[RequireComponent(typeof(MeshCollider))] // Bu scriptin çalışması için MeshCollider zorunlu.
public class ProceduralObjectPlacer : MonoBehaviour
{
    // Script aktif olduğunda anons sistemine abone ol.
    private void OnEnable()
    {
        MapGenerator.OnIslandGenerated += GenerateObjects;
    }

    // Script pasif olduğunda abonelikten çık (hafıza sızıntısını önler).
    private void OnDisable()
    {
        MapGenerator.OnIslandGenerated -= GenerateObjects;
    }

    // Inspector'dan ayarlayacağın obje listesi
    public PlaceableObject[] objectsToPlace;

    [Tooltip("Objeleri yerleştirmek için ne kadar sıklıkla örnek alınacağını belirler. Düşük değerler daha yoğun tarama yapar.")]
    [Range(0.5f, 200f)]
    public float placementStep = 10f;

    // Oluşturulan objeleri içinde toplayacak parent objelerin referansları
    private Transform objectParentContainer;

    // Bu metod, editördeki butondan veya MapGenerator'dan gelen anonsla çağrılacak
    public void GenerateObjects()
    {
        ClearObjects();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            Debug.LogWarning("Obje yerleştirme atlandı çünkü MeshCollider veya mesh henüz hazır değil. Harita oluşturulduktan sonra tekrar denenecek.");
            return;
        }
        Bounds bounds = meshCollider.bounds;

        GameObject mainParent = new GameObject("Procedurally Placed Objects");
        mainParent.transform.SetParent(this.transform);
        objectParentContainer = mainParent.transform;

        Dictionary<PlaceableObject, Transform> parentTransforms = new Dictionary<PlaceableObject, Transform>();
        foreach (PlaceableObject objType in objectsToPlace)
        {
            GameObject typeParent = new GameObject(objType.name + "s");
            typeParent.transform.SetParent(objectParentContainer);
            parentTransforms.Add(objType, typeParent.transform);
        }

        for (float x = bounds.min.x; x < bounds.max.x; x += placementStep)
        {
            for (float z = bounds.min.z; z < bounds.max.z; z += placementStep)
            {
                float sampleX = x + Random.Range(-placementStep / 2f, placementStep / 2f);
                float sampleZ = z + Random.Range(-placementStep / 2f, placementStep / 2f);

                Vector3 rayStart = new Vector3(sampleX, bounds.max.y + 10f, sampleZ);
                RaycastHit hit;

                if (Physics.Raycast(rayStart, Vector3.down, out hit, bounds.size.y + 20f) && hit.collider == meshCollider)
                {
                    // --- GÜNCELLENEN MANTIK BURADA BAŞLIYOR ---

                    float normalizedHeight = Mathf.InverseLerp(bounds.min.y, bounds.max.y, hit.point.y);

                    // 1. Bu noktaya yerleşebilecek tüm adayları ve toplam yoğunluklarını bul.
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

                    // 2. Eğer aday varsa, yoğunluklarına göre bir kura çek.
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

                        // 3. Kurayı kazanan objeyi yerleştir.
                        if (chosenObject != null)
                        {
                            Vector3 position = hit.point + new Vector3(0, chosenObject.yOffset, 0);
                            Quaternion rotation = Quaternion.identity;

                            if (chosenObject.alignToSurface)
                            {
                                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            }

                            if (chosenObject.randomRotationY)
                            {
                                rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
                            }

                            Instantiate(chosenObject.prefab, position, rotation, parentTransforms[chosenObject]);
                        }
                    }
                    // --- GÜNCELLENEN MANTIK BURADA BİTİYOR ---
                }
            }
        }
        Debug.Log("Prosedürel obje yerleştirme tamamlandı!");
    }

    public void ClearObjects()
    {
        Transform container = transform.Find("Procedurally Placed Objects");
        if (container != null)
        {
            DestroyImmediate(container.gameObject);
        }
    }
}
