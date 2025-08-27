using UnityEngine;

public class PreviewBuilding : MonoBehaviour
{
    PlayerGeneral playerGeneral;
    public GameObject buildingPrefab;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Material blockedPreviewMaterial;

    void Start()
    {
        playerGeneral = GameObject.FindWithTag("Player").GetComponent<PlayerGeneral>();
        BuildingPlace(true);
    }

    public void OnTriggerEnter(Collider other)
    {
        CheckTrigger(other);
    }

    public void OnTriggerStay(Collider other)
    {
        CheckTrigger(other);
    }

    public void OnTriggerExit(Collider other)
    {
        BuildingPlace(true);
    }

    private void CheckTrigger(Collider other)
    {
        if (other.CompareTag("BLock")) return;

        Debug.Log("Collided with: " + other.name + " Tag: " + other.tag);
        if (!other.CompareTag("Ground"))
        {
            BuildingPlace(false);
        }
        else
        {
            BuildingPlace(true);
        }
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
            Debug.LogWarning("Preview building cannot be placed here.");

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