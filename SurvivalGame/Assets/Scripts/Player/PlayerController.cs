using Unity.Cinemachine;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Input playerInputActions;
    private PlayerGeneral playerGeneral;
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
        playerGeneral = GetComponent<PlayerGeneral>();

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

        playerInputActions.Player.Run.performed -= ctx => isSprinting = true;
        playerInputActions.Player.Run.canceled -= ctx => isSprinting = false;
        playerInputActions.Player.Jump.performed -= ctx => Jump();
    }

    private void Update()
    {
        // Move eyleminden gelen Vector2 değerini okur ve moveInput değişkenine atar.
        moveInput = playerInputActions.Player.Move.ReadValue<Vector2>();

        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * transitionSpeed);
    }

    private float lastSprintTime;
    private void FixedUpdate()
    {
        float currentSpeed = moveSpeed; // Varsayılan hız olarak normal hareket hızını kullan
        if (isSprinting && playerGeneral.CurrentStamina > 0)
        {
            currentSpeed = sprintSpeed; // Sprint hızını kullan
            playerGeneral.CurrentStamina -= playerGeneral.staminaDecreaseRate * Time.fixedDeltaTime;
            if (playerGeneral.CurrentStamina <= 0)
            {
                isSprinting = false;
                lastSprintTime = Time.time;
            }
            targetFOV = 70f; // Sprint sırasında kamera FOV'sini artır
        }
        else
        {
            targetFOV = 60f; // Normal hızda FOV'yi eski haline getir
            currentSpeed = moveSpeed; // Normal hareket hızı
        }

        if (!isSprinting && playerGeneral.CurrentStamina < playerGeneral.maxStamina && Time.time - lastSprintTime > 1f)
        {
            playerGeneral.CurrentStamina += Time.fixedDeltaTime * playerGeneral.staminaIncreaseRate;
        }

        float targetYRotation = cameraTransform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, targetYRotation, 0f);

        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);
    }

    private void Jump()
    {
        if (isGrounded && playerGeneral.CurrentStamina >= 10)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerGeneral.CurrentStamina -= playerGeneral.jumpStaminaCost;
        }
    }
}
