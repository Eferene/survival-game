using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] InputAction playerInput;

    Rigidbody rb;
    public float jumpForce;
    public float speed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rb.linearVelocity = new Vector3(Input.GetAxis("Horizontal") * speed, rb.linearVelocity.y, Input.GetAxis("Vertical") * speed);
    }
}
