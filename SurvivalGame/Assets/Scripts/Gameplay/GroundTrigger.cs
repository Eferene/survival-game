using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    PlayerController playerController;
    private void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground")) playerController.isGrounded = true; // Set grounded state to true when entering ground collider

    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground")) playerController.isGrounded = false; // Set grounded state to false when exiting ground collider

    }
}
