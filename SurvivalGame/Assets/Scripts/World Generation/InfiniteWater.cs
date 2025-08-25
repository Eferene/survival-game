using System.Collections.Generic;
using UnityEngine;

public class InfiniteWater : MonoBehaviour
{
    [SerializeField] private GameObject waterTilePrefab; // Her bir su parçasını temsil eden prefab. Bunu Inspector'dan sürükleyip bırakıyorsun.
    [SerializeField] private float tileSize = 100f; // Her su tile'ının boyutu. Ölçü birimi Unity birimi (Unity unit).
    [SerializeField] private int viewDistance = 10; // Oyuncudan kaç tile uzağa kadar su oluşturulacağını belirler.
    private Transform playerTransform; // Oyuncunun pozisyonunu takip etmek için referans.

    private Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    // Şu anda sahnede aktif olan tüm su tile'larını tutar. Key = koordinat, Value = tile objesi
    private Vector2Int lastPlayerCoord; // Oyuncunun en son hangi tile'da olduğunu tutar.

    void Start()
    {
        playerTransform = Camera.main.transform;

        lastPlayerCoord = GetPlayerCoord();
        UpdateWaterTiles();
    }

    void Update()
    {
        Vector2Int currentPlayerCoord = GetPlayerCoord();

        if (currentPlayerCoord != lastPlayerCoord)
        {
            lastPlayerCoord = currentPlayerCoord;
            UpdateWaterTiles();
        }
    }

    private void UpdateWaterTiles()
    {
        // Silinecek tile'ları geçici olarak burada tutuyoruz.
        List<Vector2Int> tilesToRemove = new List<Vector2Int>();

        foreach (var tile in activeTiles)
        {
            // Eğer tile, oyuncunun görüş mesafesinin dışındaysa
            if (Mathf.Abs(tile.Key.x - lastPlayerCoord.x) > viewDistance || Mathf.Abs(tile.Key.y - lastPlayerCoord.y) > viewDistance)
            {
                tilesToRemove.Add(tile.Key);    // Silinecekler listesine ekle
                Destroy(tile.Value);            // Tile objesini sahneden kaldır
            }
        }

        foreach (var coord in tilesToRemove)
        {
            activeTiles.Remove(coord);  // Dictionary'den de kaldır
        }

        // Şimdi oyuncunun etrafında gerekli olan yeni tile'ları ekliyoruz
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int targetCoord = new Vector2Int(lastPlayerCoord.x + x, lastPlayerCoord.y + z);

                // Eğer bu koordinatta tile yoksa oluştur
                if (!activeTiles.ContainsKey(targetCoord))
                {
                    Vector3 spawnPosition = new Vector3(targetCoord.x * tileSize - 750f, -10f, targetCoord.y * tileSize - 750f);
                    GameObject newTile = Instantiate(waterTilePrefab, spawnPosition, Quaternion.identity, transform);
                    // Yeni tile'ı sahneye ekle ve bu objeyi activeTiles'a kaydet
                    activeTiles.Add(targetCoord, newTile);
                }
            }
        }
    }

    private Vector2Int GetPlayerCoord()
    {
        // Oyuncunun hangi tile'da olduğunu bulmak için pozisyonunu tileSize ile böl ve yuvarla
        int coordX = Mathf.RoundToInt(playerTransform.position.x / tileSize);
        int coordZ = Mathf.RoundToInt(playerTransform.position.z / tileSize);
        return new Vector2Int(coordX, coordZ);
    }
}
