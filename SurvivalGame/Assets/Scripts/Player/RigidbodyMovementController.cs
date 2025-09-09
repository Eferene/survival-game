using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RigidbodyMovementController : MonoBehaviour
{
    #region State & Components
    private enum PlayerState { Grounded, InAir, Swimming }
    private PlayerState currentState;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Input playerInputActions;

    [Header("Component References")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private AudioSource playerAudioSource;
    #endregion

    #region Gameplay Parameters
    [Header("Ground & Slope Handling")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;      // Karakterin altından ne kadar mesafede zemin kontrolü yapılacağını belirler.
    [SerializeField] private float groundCheckDistance = 0.1f;    // Zemin kontrolü için karakterin altına gönderilecek ışının uzunluğu.
    [SerializeField] private float slopeStickForce = 100f;        // Karakterin eğimli yüzeylerde aşağı kaymasını önlemek için yüzeyin normaline karşı uygulanan kuvvet.

    [Header("Movement - Ground")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float maxVelocityHorizontal = 15f;   // Karakterin ulaşabileceği maksimum yatay hız.
    [SerializeField] private float maxVelocityVertical = 15f;     // Karakterin ulaşabileceği maksimum dikey hız.

    [Header("Movement - Air/Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float airControlMultiplier = 0.5f;
    [SerializeField] private float extraGravityForce = 20f;       // Zıplama ve düşme hissini daha tok ve gerçekçi kılmak için standart yerçekimine ek olarak uygulanan kuvvet.

    [Header("Movement - Swimming")]
    [SerializeField] private float swimForce = 5f;
    [SerializeField] private float swimAscendSpeed = 3f;
    [SerializeField] private float swimDescendSpeed = 3f;
    [SerializeField] private float buoyancy = 10f;                // Kaldırma kuvveti.
    [SerializeField] private float swimmingCheckRadius = 0.5f;    // Karakterin su içinde olup olmadığını kontrol etmek için kullanılan küre yarıçapı.
    [SerializeField] private float swimmingCheckDistance;         // Su kontrolü için karakterin merkezinden aşağıya doğru olan mesafe.

    [Header("Audio")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private float stepSoundCooldown = 1.5f;

    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 80f;
    #endregion

    #region Private Runtime Variables
    private Vector2 moveInput;  // Oyuncunun hareket girdisini depolar.
    private bool jumpInput = false;
    private bool sprintInput = false;

    private float lastStepTime = 0f;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        playerInputActions = new Input();
        SetupInputCallbacks();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable() => playerInputActions.Player.Enable();    // Oyuncu input eylemlerini dinlemeye başlar.

    private void OnDisable() => playerInputActions.Player.Disable();   // Oyuncu input eylemlerini dinlemeyi durdurur.

    private void FixedUpdate()
    {
        UpdatePlayerState();

        // Mevcut duruma göre uygun hareket mantığını çalıştırır.
        switch (currentState)
        {
            case PlayerState.Grounded:
                HandleGroundedMovement();
                ApplySlopeStickingForce();
                break;
            case PlayerState.InAir:
                HandleAirMovement();
                ApplyExtraGravity();
                break;
            case PlayerState.Swimming:
                HandleSwimmingMovement();
                break;
        }

        HandleJumpInput();
        LimitVelocity();
        ApplyUpdateFOV();
    }
    #endregion

    #region Core Logic Methods
    // Oyuncunun durumunu, su ve zemin kontrollerine göre günceller.
    private void UpdatePlayerState()
    {
        if (IsinWater())
        {
            Debug.LogWarning("Player is swimming");
            currentState = PlayerState.Swimming;
        }
        else if (IsGrounded())
        {
            Debug.LogWarning("Player is grounded");
            currentState = PlayerState.Grounded;
        }
        else
        {
            Debug.LogWarning("Player is flying");
            currentState = PlayerState.InAir;
        }
    }

    // Karakter yerdeyken uygulanacak hareket mantığını yönetir.
    private void HandleGroundedMovement()
    {
        rb.useGravity = true;

        if (moveInput != Vector2.zero)
        {
            CallStepSound();
            rb.linearDamping = 0f;
        }
        else
            rb.linearDamping = 10f;

        float currentSpeed = sprintInput ? moveSpeed * sprintMultiplier : moveSpeed;
        rb.AddForce(GetMoveDirection() * currentSpeed, ForceMode.Force);
    }

    // Karakter havadayken uygulanacak hareket mantığını yönetir.
    private void HandleAirMovement()
    {
        rb.useGravity = true;

        if (moveInput != Vector2.zero)
            rb.linearDamping = 0f;
        else
            rb.linearDamping = 1f;

        rb.AddForce(GetMoveDirection() * moveSpeed * airControlMultiplier, ForceMode.Force);
    }

    // Karakter sudayken uygulanacak hareket mantığını yönetir.
    private void HandleSwimmingMovement()
    {
        rb.useGravity = false;

        if (moveInput != Vector2.zero)
            rb.linearDamping = 0f;
        else
            rb.linearDamping = 2f;

        Vector3 swimDirection = (playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x).normalized;
        rb.AddForce(swimDirection * swimForce, ForceMode.Force);

        rb.AddForce(Vector3.up * buoyancy, ForceMode.Force);

        if (sprintInput)
            rb.AddForce(Vector3.down * swimDescendSpeed, ForceMode.Force);
    }

    // Zıplama girdisini oyuncunun mevcut durumuna göre işler.
    private void HandleJumpInput()
    {
        if (currentState == PlayerState.Grounded && jumpInput)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpInput = false;
        }
        else if (currentState == PlayerState.Swimming && jumpInput)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, swimAscendSpeed, rb.linearVelocity.z);
    }

    // Karakterin eğimli yüzeylerde kaymasını engellemek için ek bir kuvvet uygular.
    private void ApplySlopeStickingForce()
    {
        if (moveInput == Vector2.zero) return;

        // Karakterin altındaki zemini tespit etmek için aşağı doğru bir ışın gönderir.
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, (capsuleCollider.height / 2) + 2f, groundLayer))
        {
            if (hit.normal != Vector3.up)
            {
                rb.AddForce(-hit.normal * slopeStickForce, ForceMode.Force);
            }
        }
    }

    // Havadayken daha hızlı düşmesini sağlamak için ek bir yerçekimi kuvveti uygular.
    private void ApplyExtraGravity()
    {
        rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
    }

    private void LimitVelocity()
    {
        float currentMaxVelocityHorizontal = sprintInput ? maxVelocityHorizontal * sprintMultiplier : maxVelocityHorizontal;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 verticalVelocity = new Vector3(0, rb.linearVelocity.y, 0);

        if (horizontalVelocity.magnitude > currentMaxVelocityHorizontal)
        {
            // Hız vektörünü, yönünü koruyarak maksimum hız büyüklüğüne indirger.
            Vector3 limitedVelocity = horizontalVelocity.normalized * currentMaxVelocityHorizontal;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }

        if (verticalVelocity.magnitude > maxVelocityVertical)
        {
            Vector3 limitedVelocity = verticalVelocity.normalized * maxVelocityVertical;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, limitedVelocity.y, rb.linearVelocity.z);
        }
    }

    private void ApplyUpdateFOV()
    {
        float targetFOV = sprintInput && !IsinWater() ? sprintFOV : normalFOV;
        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * 5f);
    }

    private void CallStepSound()
    {
        float currentStepSoundCooldown = sprintInput ? stepSoundCooldown / sprintMultiplier : stepSoundCooldown;

        if (Time.time - lastStepTime >= currentStepSoundCooldown)
        {
            playerAudioSource.PlayOneShot(walkSound);
            lastStepTime = Time.time;
        }
    }
    #endregion

    #region Helper & Utility Methods
    private void SetupInputCallbacks()
    {
        playerInputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        playerInputActions.Player.Jump.performed += ctx => jumpInput = true;
        playerInputActions.Player.Jump.canceled += ctx => jumpInput = false;

        playerInputActions.Player.Run.performed += ctx => sprintInput = true;
        playerInputActions.Player.Run.canceled += ctx => sprintInput = false;
    }

    private bool IsinWater()
    {
        Vector3 spherePosition = transform.position + Vector3.down * swimmingCheckDistance;
        // Karakterde "Water" katmanıyla temas eden bir küre olup olmadığını kontrol eder.
        return Physics.CheckSphere(spherePosition, swimmingCheckRadius, LayerMask.GetMask("Water"), QueryTriggerInteraction.Collide);
    }

    private bool IsGrounded()
    {
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckDistance;
        // Karakterde groundLayer katmanıyla temas eden bir küre olup olmadığını kontrol eder.
        return Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    // Oyuncu girdisine ve kamera açısına göre hedeflenen hareket yönünü hesaplar.
    private Vector3 GetMoveDirection()
    {
        // Kameranın ileri ve sağ yön vektörlerini alır.
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        // Girdi ve kamera yönlerini birleştirip normalize ederek son hareket yönü vektörünü oluşturur.
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }
    #endregion

    #region Editor Methods
    // Unity Editor'da çalışırken, seçili olan bu objenin etrafına yardımcı görseller (Gizmo) çizer.
    private void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        // IsGrounded metodunda kullanılan kürenin konumunu ve boyutunu editörde görselleştirir.
        Gizmos.color = Color.yellow;
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);

        // IsinWater metodunda kullanılan kürenin konumunu ve boyutunu editörde görselleştirir.
        Gizmos.color = Color.blue;
        Vector3 spherePosition2 = transform.position + Vector3.down * swimmingCheckDistance;
        Gizmos.DrawWireSphere(spherePosition2, swimmingCheckRadius);
    }
    #endregion
}