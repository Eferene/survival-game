using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerGeneral))]
public class PlayerControllerOld : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private CinemachineCamera playerCamera;

    [Header("Look Settings")]

    [Header("Movement Settings")]
    private float moveSpeed;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    //[SerializeField] private float jumpForce = 10f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Swimming Settings")]
    [SerializeField] private float swimSpeedMultiplier = 0.7f;
    [SerializeField] private float swimAscendSpeed = 3f;

    [Header("Stamina Settings")]
    [SerializeField] private float staminaRegenDelay = 1f;

    [Header("FOV Settings")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float fovTransitionSpeed = 5f;

    public bool isInWater = false;

    private Rigidbody rb;
    private PlayerGeneral playerGeneral;
    private GroundTrigger groundTrigger;
    private Input playerInputActions;

    private Vector2 moveInput;
    private bool isSprinting = false;
    private bool isJumpPressing = false;
    private float targetFOV;
    private float lastSprintTime;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerGeneral = GetComponent<PlayerGeneral>();
        groundTrigger = GetComponent<GroundTrigger>();
        playerInputActions = new Input();

        targetFOV = defaultFOV;
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();

        // Bellek sızıntılarını (memory leak) önlemek için, OnEnable içerisinde abone olunan olaylar (events),
        // obje pasif hale geçtiğinde veya yok edildiğinde OnDisable içerisinde abonelikten çıkarılmalıdır.
        playerInputActions.Player.Run.performed += OnRunPerformed;
        playerInputActions.Player.Run.canceled += OnRunCanceled;
        playerInputActions.Player.Jump.performed += OnJumpPerformed;
        playerInputActions.Player.Jump.canceled += OnJumpCanceled;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Run.performed -= OnRunPerformed;
        playerInputActions.Player.Run.canceled -= OnRunCanceled;
        playerInputActions.Player.Jump.performed -= OnJumpPerformed;
        playerInputActions.Player.Jump.canceled -= OnJumpCanceled;

        playerInputActions.Player.Disable();
    }

    private void Update()
    {
        moveInput = playerInputActions.Player.Move.ReadValue<Vector2>();

        if (isInWater)
        {
            //groundTrigger.isGrounded = false;
        }

        UpdateFOV();
    }

    private void FixedUpdate()
    {
        // Fizik hesaplamaları, oyunun anlık kare hızından (frame rate) etkilenmeden
        // tutarlı bir şekilde çalışması için FixedUpdate içerisinde gerçekleştirilir.
        HandleSprintAndStamina();
        ApplyRotation();
        ApplyMovement();
        HandleSwimming();
    }

    private void OnRunPerformed(InputAction.CallbackContext context) => isSprinting = true;
    private void OnRunCanceled(InputAction.CallbackContext context) => isSprinting = false;

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        //if (groundTrigger.isGrounded)
        //{
        //    Jump();
        //}
        //else if (isInWater)
        //{
        //    isJumpPressing = true;
        //}
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        isJumpPressing = false;
    }

    private void ApplyMovement()
    {
        moveSpeed = isSprinting ? sprintSpeed : walkSpeed;
        if (isInWater) moveSpeed *= swimSpeedMultiplier;

        Vector3 moveDirection;

        // Hareket yönü, karakterin suda veya karada olmasına göre farklı şekilde belirlenir.
        if (isInWater)
        {
            // Suda hareket yönü, kameranın baktığı yöne göre 3 boyutlu olarak belirlenir.
            moveDirection = (playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x).normalized;
        }
        else
        {
            // Karada ise hareket, karakterin kendi yerel X ve Z eksenlerine göre hesaplanır.
            moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        }

        Vector3 targetVelocity = moveDirection * moveSpeed;

        // Karakter yerdeyken, yatay hareketin dikey hızı (zıplama, düşme) etkilememesi için Rigidbody'nin mevcut Y ekseni hızı korunur.
        if (!isInWater)
        {
            targetVelocity.y = rb.linearVelocity.y;
        }

        // Rigidbody'nin mevcut hızını doğrudan değiştirmek yerine, hedeflenen hıza ulaşmak için anlık bir kuvvet uygulanır.
        // ForceMode.VelocityChange, objenin kütlesini göz ardı ederek anlık bir hız değişimi sağlar ve daha tepkisel bir kontrol hissi yaratır.
        Vector3 velocityChange = (targetVelocity - rb.linearVelocity);
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void ApplyRotation()
    {
        float targetYRotation = playerCamera.transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // rb.MoveRotation, Rigidbody'nin dönüşünü fizik simülasyonu ile uyumlu bir şekilde günceller.
        // Quaternion.Slerp, mevcut rotasyondan hedef rotasyona yumuşak, küresel bir geçiş yaparak ani dönüşleri engeller.
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
    }

    private void Jump()
    {
        //if (groundTrigger.isGrounded && !isInWater && playerGeneral.CurrentStamina >= playerGeneral.jumpStaminaCost)
        //{
        //    // ForceMode.Impulse, objenin kütlesi de hesaba katılarak anlık bir itki kuvveti uygular. Bu mod, zıplama gibi anlık eylemler için idealdir.
        //    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //    playerGeneral.CurrentStamina -= playerGeneral.jumpStaminaCost;
        //}
    }

    private void HandleSwimming()
    {
        if (isInWater)
        {
            rb.useGravity = false;

            // Yüzme sırasında zıplama tuşuna basıldığında, karakterin yukarı yönde yüzmesi için Y eksenindeki hızı sabit bir değere ayarlanır.
            if (isJumpPressing && playerGeneral.CurrentStamina > 0)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, swimAscendSpeed, rb.linearVelocity.z);
            }
        }
        else
        {
            rb.useGravity = true;
        }
    }

    private void HandleSprintAndStamina()
    {
        if (isSprinting && moveInput.magnitude > 0.1f && playerGeneral.CurrentStamina > 0)
        {
            playerGeneral.CurrentStamina -= playerGeneral.staminaDecreaseRate * Time.fixedDeltaTime;
            targetFOV = sprintFOV;
            lastSprintTime = Time.time;

            if (playerGeneral.CurrentStamina <= 0)
            {
                playerGeneral.CurrentStamina = 0;
                isSprinting = false;
            }
        }
        else
        {
            targetFOV = defaultFOV;

            // Stamina yenilenmesi, son sprint eyleminden bu yana belirli bir gecikme süresi (staminaRegenDelay) geçtikten sonra başlar.
            if (playerGeneral.CurrentStamina < playerGeneral.maxStamina && Time.time - lastSprintTime > staminaRegenDelay)
            {
                playerGeneral.CurrentStamina += playerGeneral.staminaIncreaseRate * Time.fixedDeltaTime;
                // Stamina değerinin maksimum değeri aşmamasını sağlar.
                playerGeneral.CurrentStamina = Mathf.Min(playerGeneral.CurrentStamina, playerGeneral.maxStamina);
            }
        }
    }

    private void UpdateFOV()
    {
        if (playerCamera != null)
        {
            // Mathf.Lerp (Lineer İnterpolasyon), mevcut görüş alanından (FOV) hedef görüş alanına belirlenen hızda yumuşak bir geçiş yapılmasını sağlar.
            playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
        }
    }
}