using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private CinemachineCamera playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float groundJumpForce = 10f;
    [SerializeField] private float slopeJumpForce = 100f;
    [SerializeField] private float maxSpeed = 15f;
    private float currentSpeed;
    private float currentJumpForce;

    [Header("Swimming Settings")]
    [SerializeField] private float swimSpeedMultiplier = 0.5f;
    [SerializeField] private float swimAscendSpeed = 1f;
    [SerializeField] private float swimDescendSpeed = 0.5f;

    [Header("Stamina Settings")]
    [SerializeField] private float staminaRegenDelay = 1f;

    [Header("FOV Settings")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float fovTransitionSpeed = 5f;
    private float currentFOV;

    public bool isInWater = false;
    public bool isGrounded = false;

    private Rigidbody rb;
    private PlayerGeneral playerGeneral;
    private GroundTrigger groundTrigger;
    private Input playerInputActions;

    private Vector2 movementInput;
    private bool isSprinting = false;
    private bool jumpPressed = false;
    private float lastSprintTimestamp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerGeneral = GetComponent<PlayerGeneral>();
        groundTrigger = GetComponent<GroundTrigger>();
        playerInputActions = new Input();

        currentFOV = defaultFOV;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.Jump.performed += OnJumpPerformed;
        playerInputActions.Player.Jump.canceled += OnJumpCanceled;
        playerInputActions.Player.Run.performed += OnRunPerformed;
        playerInputActions.Player.Run.canceled += OnRunCanceled;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Jump.performed -= OnJumpPerformed;
        playerInputActions.Player.Jump.canceled -= OnJumpCanceled;
        playerInputActions.Player.Run.performed -= OnRunPerformed;
        playerInputActions.Player.Run.canceled -= OnRunCanceled;
        playerInputActions.Player.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx) => Jump();
    private void OnJumpCanceled(InputAction.CallbackContext ctx) => CancelJump();
    private void OnRunPerformed(InputAction.CallbackContext ctx) => isSprinting = true;
    private void OnRunCanceled(InputAction.CallbackContext ctx) => isSprinting = false;

    private void Update()
    {
        movementInput = playerInputActions.Player.Move.ReadValue<Vector2>();

        UpdateFOV();
        HandleSprintAndStamina();
    }

    private void FixedUpdate()
    {
        Vector3 rbHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 rbVertical = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (rbHorizontal.magnitude > maxSpeed)
        {
            // Eğer hız maksimumdan fazlaysa, hızı sınırla
            rb.linearVelocity = new Vector3(
                rbHorizontal.x * maxSpeed,
                rb.linearVelocity.y, // Y eksenindeki hızı koru (zıplama ve yerçekimi için)
                rbHorizontal.z * maxSpeed
            ).normalized;
        }

        if (rbVertical.magnitude > maxSpeed)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x, // X eksenindeki hızı koru
                rbVertical.y * maxSpeed,
                rb.linearVelocity.z // Z eksenindeki hızı koru
            ).normalized;
        }

        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        // Hız belirlemesi: koşma ve stamina durumuna göre değişir
        currentSpeed = isSprinting && playerGeneral.CurrentStamina > 0 ? sprintSpeed : walkSpeed;

        rb.useGravity = !isInWater; // Suda yerçekimini kapat

        Vector3 movementDirection;

        if (isInWater)
        {
            currentSpeed *= swimSpeedMultiplier;
            // Kamera yönünde hareket (yüzme için)
            movementDirection = (playerCamera.transform.forward * movementInput.y + playerCamera.transform.right * movementInput.x).normalized;
        }
        else if (groundTrigger.CheckIfOnSlope())
        {
            Vector3 inputDirection = (transform.forward * movementInput.y + transform.right * movementInput.x).normalized;
            Vector3 slopeNormal = groundTrigger.HitInfo.normal;

            // Hareket yönünü eğim düzlemine projekte et, böylece eğim boyunca düzgün hareket sağlanır
            movementDirection = Vector3.ProjectOnPlane(inputDirection, slopeNormal).normalized;

            rb.AddForce(Vector3.down * 50f, ForceMode.Acceleration);

            // Eğim açısı 45° üstündeyse yokuş yukarı çıkışı engelle
            if (groundTrigger.slopeAngle > 45f)
            {
                // Eğim üzerinde "yukarı" yönü bulmak için çapraz çarpım kullanılır
                Vector3 slopeUpDirection = Vector3.Cross(Vector3.Cross(Vector3.up, slopeNormal), slopeNormal).normalized;

                // Vector3.Dot ile hareket yönünün eğim yukarısına mı olduğunu kontrol et
                // Dot < 0 ise hareket yukarı doğru
                if (Vector3.Dot(movementDirection, slopeUpDirection) < 0)
                {
                    if (groundTrigger.slopeAngle > 60f)
                        movementDirection /= 2.5f;
                    else if (groundTrigger.slopeAngle > 50f)
                        movementDirection /= 2f;
                    else
                        movementDirection /= 1.5f;
                }
            }
        }
        else
            movementDirection = (transform.forward * movementInput.y + transform.right * movementInput.x).normalized;

        Vector3 targetVelocity = movementDirection * currentSpeed;

        if (!isInWater)
            // Karada, mevcut dikey hızı koru (zıplama ve yerçekimi için)
            targetVelocity.y = rb.linearVelocity.y;
        else
            // Suda yüzerken yukarı/aşağı hareket için Y hızını ayarla
            targetVelocity.y += jumpPressed ? swimAscendSpeed : -swimDescendSpeed;

        // Rigidbody hızını hedef hıza eşitle (ani hız değişimi için VelocityChange kullanıyoruz)
        rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.VelocityChange);
    }

    private void HandleRotation()
    {
        // Kamera yönüne göre oyuncuyu yatayda döndür
        float desiredYRotation = playerCamera.transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, desiredYRotation, 0f);

        // Slerp ile yumuşak dönüş
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 15f));
    }

    private void Jump()
    {
        currentJumpForce = groundTrigger.CheckIfOnSlope() ? slopeJumpForce : groundJumpForce;

        jumpPressed = true;

        // Yerdeysek, suda değilsek ve yeterli stamina varsa zıpla
        if (isGrounded && !isInWater && playerGeneral.CurrentStamina > playerGeneral.jumpStaminaCost)
        {
            rb.AddForce(Vector3.up * currentJumpForce, ForceMode.VelocityChange);
            playerGeneral.CurrentStamina -= playerGeneral.jumpStaminaCost;
        }
    }

    private void CancelJump()
    {
        jumpPressed = false;
    }

    private void HandleSprintAndStamina()
    {
        // Koşuyorsa ve stamina varsa
        if (isSprinting && playerGeneral.CurrentStamina > 0)
        {
            // Koşarken stamina azalır
            playerGeneral.CurrentStamina -= Time.deltaTime * playerGeneral.staminaDecreaseRate;
            lastSprintTimestamp = Time.time;

            if (playerGeneral.CurrentStamina <= 0)
            {
                playerGeneral.CurrentStamina = 0;
                isSprinting = false;
            }
        }
        else
        {
            // Koşmuyorsa ve stamina dolum süresi geçtiyse, stamina artar
            if (Time.time - lastSprintTimestamp > staminaRegenDelay)
            {
                playerGeneral.CurrentStamina += Time.deltaTime * playerGeneral.staminaIncreaseRate;
            }
        }
    }

    private void UpdateFOV()
    {
        // Koşarken görüş alanı genişler, koşmazken eski haline döner
        currentFOV = isSprinting ? sprintFOV : defaultFOV;
        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, currentFOV, Time.deltaTime * fovTransitionSpeed);
    }
}
