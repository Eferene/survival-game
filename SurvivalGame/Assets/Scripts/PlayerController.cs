using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Input playerInputActions;
    private Rigidbody rb;
    private Vector2 moveInput;

    [Header("Movement Settings")]
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 10f;
    //[SerializeField] float fallMultiplier = 5f;

    [Header("Camera & Look Settings")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] CinemachineCamera playerCamera;

    private bool isSprinting = false;
    public bool isGrounded = false;

    private float targetFOV = 60f; // Hedef FOV değeri
    [SerializeField] private float transitionSpeed = 5f; // FOV geçiş hızı

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

        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * transitionSpeed);
    }

    private void FixedUpdate()
    {
        float currentSpeed = moveSpeed; // Varsayılan hız olarak normal hareket hızını kullan
        if (isSprinting)
        {
            if (!isGrounded)
            {
                isSprinting = false; // Yerde değilse sprinti kapat
            }
            currentSpeed = sprintSpeed; // Sprint hızını kullan
            targetFOV = 70f; // Sprint sırasında kamera FOV'sini artır
        }else 
        {
            targetFOV = 60f; // Normal hızda FOV'yi eski haline getir
            currentSpeed = moveSpeed; // Normal hareket hızı
        } 

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
