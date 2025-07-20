using UnityEngine;

public class Object : MonoBehaviour
{
    public ItemData item;
    public int quantity;

    public bool physics;
    public bool inHand;

    public void SetPhysicsEnabled(bool enabled)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>();
        if (rb != null)
        {
            rb.isKinematic = !enabled;
            rb.useGravity = enabled;
        }
        if (col != null)
        {
            col.enabled = enabled;
        }
        physics = enabled;
    }
}
