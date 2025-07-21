using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PlaceableObject
{
    public string name;
    public GameObject prefab;
    [Range(0f, 1f)] public float minHeight = 0f;
    [Range(0f, 1f)] public float maxHeight = 1f;
    [Range(0f, 100f)] public float density = 10f;
    public float yOffset = 0f;
    public bool alignToSurface = false;
    public bool randomRotationY = true;
}

[RequireComponent(typeof(MeshCollider))]
public class ProceduralObjectPlacer : MonoBehaviour
{
    [Tooltip("Objeleri yerleştirmek için harita verisini sağlayacak olan MapGenerator.")]
    public MapGenerator mapGenerator;

    public PlaceableObject[] objectsToPlace;
    [Range(0.5f, 200f)]
    public float placementStep = 10f;

    private float[,] heightMap;
    private Transform objectParentContainer;
    [SerializeField] private GameObject environment;

    private void OnEnable()
    {
        MapGenerator.OnIslandGenerated += HandleMapGeneration;
    }

    private void OnDisable()
    {
        MapGenerator.OnIslandGenerated -= HandleMapGeneration;
    }

    public void HandleMapGeneration(MapData mapData)
    {
        this.heightMap = mapData.heightMap;

        // DEĞİŞTİ: Oyun modunda Coroutine, editörde delayCall kullanılıyor.
        // Bu, MeshCollider'ın güncellenmesi için zaman tanır.
        if (Application.isPlaying)
        {
            StartCoroutine(PlaceObjectsAfterFrame());
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += PlaceObjects;
#endif
        }
    }

    // YENİ: Oyun modunda bir frame bekleyip objeleri yerleştiren Coroutine.
    private IEnumerator PlaceObjectsAfterFrame()
    {
        yield return null;
        PlaceObjects();
    }

    public void PlaceObjects()
    {
        ClearObjects();

        if (mapGenerator != null)
        {
            Debug.Log("MapGenerator'dan en güncel harita verisi isteniyor...");
            MapData? data = mapGenerator.GetLastGeneratedMapData();
            if (data.HasValue)
            {
                this.heightMap = data.Value.heightMap;
            }
        }

        if (this.heightMap == null)
        {
            Debug.LogError("HeightMap alınamadı! Önce haritayı oluşturun ve 'Map Generator' referansını atayın.");
            return;
        }

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            Debug.LogWarning("Obje yerleştirme atlandı çünkü MeshCollider veya mesh hazır değil. Önce harita oluşturun.");
            return;
        }

        Bounds bounds = meshCollider.bounds;

        GameObject mainParent = new GameObject("Procedurally Placed Objects " + gameObject.name);
        mainParent.transform.SetParent(environment.transform);
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

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, bounds.size.y + 20f, 1, QueryTriggerInteraction.Ignore) && hit.collider == meshCollider)
                {
                    float normalizedHeight = MapGenerator.GetBilinearInterpolatedHeight(this.heightMap, hit.textureCoord.x, hit.textureCoord.y);

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
        Debug.Log("Prosedürel obje yerleştirme tamamlandı!");
    }

    public void ClearObjects()
    {
        string parentName = "Procedurally Placed Objects " + gameObject.name;

#if UNITY_EDITOR
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