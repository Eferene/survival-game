using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PreviewBuilding : MonoBehaviour
{
    PlayerGeneral playerGeneral;
    public GameObject buildingPrefab;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Material blockedPreviewMaterial;
    public BuildingType buildingType;
    public Vector3 offsetMultiplier = Vector3.one;

    private List<Collider> collidingObjects = new List<Collider>();

    void Start()
    {
        playerGeneral = GameObject.FindWithTag("Player").GetComponent<PlayerGeneral>();
        BuildingPlace(true);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!collidingObjects.Contains(other))
        {
            collidingObjects.Add(other);
        }

        CheckAllCollisions();
    }

    public void OnTriggerStay(Collider other)
    {
        CheckAllCollisions();
    }

    public void OnTriggerExit(Collider other)
    {
        if (collidingObjects.Contains(other))
        {
            collidingObjects.Remove(other);
        }

        CheckAllCollisions();
    }

    private Vector3Int ToGridKey(Vector3 pos, float gridSize = 0.05f)
    {
        return new Vector3Int(
            Mathf.RoundToInt(pos.x / gridSize),
            Mathf.RoundToInt(pos.y / gridSize),
            Mathf.RoundToInt(pos.z / gridSize)
        );
    }

    private void CheckAllCollisions()
    {
        bool canPlace = true;

        Dictionary<Vector3Int, Collider> uniqueByPos = new Dictionary<Vector3Int, Collider>();
        float gridSize = 0.05f;

        foreach (Collider col in collidingObjects)
        {
            Vector3Int key = ToGridKey(col.transform.position, gridSize);
            if (!uniqueByPos.ContainsKey(key))
            {
                uniqueByPos.Add(key, col);
            }
        }

        foreach (Collider col in uniqueByPos.Values)
        {
            if (col.gameObject.CompareTag("Ground") || col.gameObject.CompareTag("BLock") || col.gameObject.CompareTag("WLock") || col.gameObject.CompareTag("BuildingWall"))
            {
                continue;
            }
            canPlace = false;
            break;
        }

        if (buildingType == BuildingType.Wall)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.2f);
            foreach(Collider hitCollider in hitColliders)
            {
                if (hitCollider.gameObject.CompareTag("BuildingWall"))
                {
                    canPlace = false;
                    break;
                }
            }
        }

        BuildingPlace(canPlace);
    }

    private void BuildingPlace(bool canPlace)
    {
        playerGeneral.canBuilding = canPlace;

        if (canPlace)
        {
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<Renderer>() != null)
                    {
                        child.GetComponent<Renderer>().material = previewMaterial;
                    }
                }
            }
            if (GetComponent<Renderer>() != null)
            {
                GetComponent<Renderer>().material = previewMaterial;
            }
        }
        else
        {
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<Renderer>() != null)
                    {
                        child.GetComponent<Renderer>().material = blockedPreviewMaterial;
                    }
                }
            }
            if (GetComponent<Renderer>() != null)
            {
                GetComponent<Renderer>().material = blockedPreviewMaterial;
            }
        }
    }
}

public enum BuildingType
{
    Wall,
    Other
}