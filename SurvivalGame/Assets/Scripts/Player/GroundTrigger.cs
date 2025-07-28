using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    PlayerController playerController;
    [SerializeField] private int groundLayerCount = 0;
    private void Start()
    {
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            groundLayerCount++;
            if (groundLayerCount == 1)
                playerController.isGrounded = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            groundLayerCount--;
            if (groundLayerCount <= 0)
            {
                playerController.isGrounded = false;
                groundLayerCount = 0;
            }

        }
    }
}
