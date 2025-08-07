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
    [SerializeField] private float jumpForce = 10f;
    private float moveSpeed;

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

    private Vector2 moveInput;
    private bool isSprinting = false;
    private bool isJumpPressing = false;
    private float lastSprintTime;

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
        moveInput = playerInputActions.Player.Move.ReadValue<Vector2>();

        UpdateFOV();
        HandleSprintAndStamina();
    }

    private void FixedUpdate()
    {
        Movement();
        Rotation();
    }

    private void Movement()
    {
        moveSpeed = isSprinting && playerGeneral.CurrentStamina > 0 ? sprintSpeed : walkSpeed;
        rb.useGravity = !isInWater; // Gravity'yi sadece suda değilken kullan

        Vector3 moveDirection;

        if (isInWater)
        {
            moveSpeed *= swimSpeedMultiplier;
            moveDirection = (playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x).normalized;
        }
        else if (groundTrigger.OnSlope())
        {
            Vector3 slopeMoveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            Vector3 slopeNormal = groundTrigger.HitInfo.normal;

            moveDirection = Vector3.ProjectOnPlane(slopeMoveDirection, slopeNormal).normalized;

            //rb.AddForce(Vector3.up * 0.1f, ForceMode.VelocityChange); // Slope üzerinde kaymayı önlemek için küçük bir yukarı kuvvet ekleyin
        }
        else
        {
            moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        }

        Vector3 targetVelocity = moveDirection * moveSpeed;

        if (!isInWater)
            // Y eksenindeki hızı koruyarak yatay hareketi etkiler
            targetVelocity.y = rb.linearVelocity.y;
        else
            // Suda iken, yukarı doğru yüzme için Y eksenindeki hızı artırabiliriz
            targetVelocity.y += isJumpPressing ? swimAscendSpeed : -swimDescendSpeed;

        rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.VelocityChange);
    }

    private void Rotation()
    {
        // Kamera yönündeki Y rotasyonunu al
        float targetYRotation = playerCamera.transform.eulerAngles.y;

        // Sadece yatay eksende döndürmek için (X ve Z sabit)
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // Rigidbody’yi smooth şekilde döndür
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 15f));
    }

    private void Jump()
    {
        isJumpPressing = true;
        if (isGrounded && !isInWater && playerGeneral.CurrentStamina > playerGeneral.jumpStaminaCost)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            playerGeneral.CurrentStamina -= playerGeneral.jumpStaminaCost;
        }
    }

    private void HandleSprintAndStamina()
    {
        if (isSprinting && playerGeneral.CurrentStamina > 0)
        {
            playerGeneral.CurrentStamina -= Time.deltaTime * playerGeneral.staminaDecreaseRate;
            lastSprintTime = Time.time;

            if (playerGeneral.CurrentStamina <= 0)
            {
                playerGeneral.CurrentStamina = 0;
                isSprinting = false;
            }
        }
        else
        {
            if (Time.time - lastSprintTime > staminaRegenDelay)
            {
                playerGeneral.CurrentStamina += Time.deltaTime * playerGeneral.staminaIncreaseRate;
            }
        }
    }

    private void CancelJump()
    {
        isJumpPressing = false;
    }

    private void UpdateFOV()
    {
        currentFOV = isSprinting ? sprintFOV : defaultFOV;
        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, currentFOV, Time.deltaTime * fovTransitionSpeed);
    }
}
