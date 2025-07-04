using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Input playerInputActions;
    private Rigidbody rb;
    private Vector2 moveInput;
    [SerializeField] private Collider groundCollider;

    [Header("Movement Settings")]
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 10f;
    //[SerializeField] float fallMultiplier = 5f;

    [Header("Camera & Look Settings")]
    [SerializeField] Transform cameraTransform;

    private bool isSprinting = false;
    public bool isGrounded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerInputActions = new Input();
    }

    // Script'in ilk kez aktif olduğu anda çalışır.
    private void OnEnable()
    {
        // Player action map'ini aktif eder.
        playerInputActions.Player.Enable();

        playerInputActions.Player.Run.performed += ctx => isSprinting = true;
        playerInputActions.Player.Run.canceled += ctx => isSprinting = false;
        playerInputActions.Player.Jump.performed += ctx => Jump();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Script'in ilk kez aktif olduğu anda çalışır.
    private void OnDisable()
    {
        // Player action map'ini devre dışı bırak.
        // Oyun durunca veya karakter ölünce kaynakları serbest bırakmak amacıyla.
        playerInputActions.Player.Disable();

        playerInputActions.Player.Run.performed += ctx => isSprinting = true;
        playerInputActions.Player.Run.canceled += ctx => isSprinting = false;
        playerInputActions.Player.Jump.performed -= ctx => Jump();
    }

    private void Update()
    {
        // Move eyleminden gelen Vector2 değerini okur ve moveInput değişkenine atar.
        moveInput = playerInputActions.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed; // Sprint yapılıyorsa hızı artırıyoruz.

        // Kameranın ileri ve sağ yönlerini alıyoruz
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        // Karakter yukarı aşağı hareket etmemesi için Y eksenindeki bileşenleri sıfırlıyoruz.
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Hareket vektörünü kameranın yönüne göre hesaplıyoruz.
        Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        // Rigidbody'e hızı uyguluyoruz.
        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);

        // Eğer karakter hareket ediyorsa, hareket ettiği yöne baksın.
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 15f); // 15f değeriyle dönüş hızını ayarla
        }
    }

    private void Jump()
    {
        if (isGrounded) rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    //public bool GetIsGrounded()
    //{
    //    return isGrounded;
    //}
}
