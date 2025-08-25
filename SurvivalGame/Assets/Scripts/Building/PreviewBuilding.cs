using UnityEngine;

public class PreviewBuilding : MonoBehaviour
{
    PlayerGeneral playerGeneral;
    private void Start()
    {
        playerGeneral = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerGeneral>();
        if (GetComponent<Collider>() != null) GetComponent<Collider>().isTrigger = true;
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null) child.GetComponent<MeshRenderer>().material = playerGeneral.previewMaterial;
            }
        }
        else
        {
            GetComponent<MeshRenderer>().material = playerGeneral.previewMaterial;
        }
        gameObject.layer = 2;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Ground")
        {
            playerGeneral.previewTrigger = true;
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<MeshRenderer>() != null) child.GetComponent<MeshRenderer>().material = playerGeneral.blockedPreviewMaterial;
                }
            }
            else
            {
                GetComponent<MeshRenderer>().material = playerGeneral.blockedPreviewMaterial;
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Ground")
        {
            playerGeneral.previewTrigger = false;
            if (transform.childCount > 0)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<MeshRenderer>() != null) child.GetComponent<MeshRenderer>().material = playerGeneral.previewMaterial;
                }
            }
            else
            {
                GetComponent<MeshRenderer>().material = playerGeneral.previewMaterial;
            }
        }
    }
}