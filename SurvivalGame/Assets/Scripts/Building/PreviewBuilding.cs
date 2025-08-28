using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PreviewBuilding : MonoBehaviour
{
    PlayerGeneral playerGeneral;
    public GameObject buildingPrefab;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Material blockedPreviewMaterial;

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

    private void CheckAllCollisions()
    {
        bool canPlace = true;

        if (collidingObjects.Count > 1)
        {
            foreach (Collider col in collidingObjects)
            {
                if(col.gameObject.CompareTag("Ground") || col.gameObject.CompareTag("Building"))
                {
                    continue;
                }
                canPlace = false;
                break;
            }
        }
        else if (collidingObjects.Count == 1)
        {
            Collider col = collidingObjects[0];
            if (col.gameObject.CompareTag("Ground") || col.gameObject.CompareTag("Building"))
            {
                canPlace = true;
            }
            else
            {
                canPlace = false;
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