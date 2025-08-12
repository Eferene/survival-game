using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    PlayerController playerController;
    RigidbodyMovementController rigidbodyMovementController;
    void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        rigidbodyMovementController = GameObject.FindGameObjectWithTag("Player").GetComponent<RigidbodyMovementController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            playerController.isInWater = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            playerController.isInWater = false;
        }
    }
}
