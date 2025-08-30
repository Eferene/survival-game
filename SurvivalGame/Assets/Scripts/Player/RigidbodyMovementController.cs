using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// GameObject'te Rigidbody ve CapsuleCollider bileşenlerini zorunlu kılar.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RigidbodyMovementController : MonoBehaviour
{
    #region State & Referanslar

    private enum PlayerState { Grounded, InAir, Swimming }  // Oyuncunun bulunabileceği farklı fiziksel durumları tanımlar.
    private PlayerState currentState;                       // Oyuncunun mevcut durumunu tutar.

    private Rigidbody rb;                                   // Fizik etkileşimleri ve hareket için kullanılan Rigidbody component'inin referansı.
    private CapsuleCollider capsuleCollider;                // Oyuncunun fiziksel sınırlarını ve çarpışmalarını yöneten CapsuleCollider component'inin referansı.
    private Input playerInputActions;                       // Unity'nin Input System'i üzerinden oyuncu girdilerini yönetmek için kullanılan ana sınıfın referansı.
    [SerializeField] private CinemachineCamera playerCamera;// Oyuncuyu takip eden ve hareket yönünü belirlemede kullanılan Cinemachine sanal kamerasının referansı.
    [SerializeField] private AudioSource playerAudioSource; // Oyuncunun ses efektlerini çalmak için kullanılan AudioSource bileşeninin referansı.
    [SerializeField] private AudioClip walkSound;           // Yürüme anında çalınacak ses efekti.
    #endregion

    #region Hareket Parametreleri (Inspector'dan Ayarlanabilir)
    [Header("Movement - Ground")]

    [SerializeField] private float moveSpeed = 8f;              // Karakterin yerdeki standart yürüme hızı.
    [SerializeField] private float sprintSpeedMultipler = 12f;  // Karakterin sprint durumundaki hız çarpanı.
    [SerializeField] private float maxVelocity = 15f;           // Karakterin ulaşabileceği maksimum yatay hız. Bu, hızın kontrolsüzce artmasını önler.
    [SerializeField] private float dragOnGround = 6f;           // Karakter yerdeyken uygulanan sürtünme kuvveti. Hareketi durdurduğunda yavaşça kaymasını engeller.

    [Header("Movement - Air/Jump")]
    [SerializeField] private float jumpForce = 8f;              // Zıplama anında karaktere uygulanan anlık dikey kuvvet.
    [SerializeField] private float airControlMultiplier = 0.5f; // Karakter havadayken hareket kontrolünün ne kadar etkili olacağını belirleyen çarpandır.
    [SerializeField] private float dragInAir = 0.5f;            // Karakter havadayken uygulanan sürtünme. Genellikle yerdeki sürtünmeden daha düşüktür.

    [Header("Movement - Swimming")]
    [SerializeField] private float swimForce = 5f;              // Karakter sudayken hareket etmek için uygulanan kuvvet.
    [SerializeField] private float swimAscendSpeed = 3f;        // Karakterin su yüzeyine doğru yüzmesini sağlayan yukarı doğru hareket hızı.
    [SerializeField] private float buoyancy = 10f;              // Karakteri suyun yüzeyine doğru iten kaldırma kuvveti.
    [SerializeField] private float dragInWater = 3f;            // Karakter sudayken hareketine karşı koyan sürtünme kuvveti.
    [SerializeField] private float swimmingCheckRadius = 0.5f;  // Karakterin su içinde olup olmadığını kontrol etmek için kullanılan küre yarıçapı.
    [SerializeField] private float swimmingCheckDistance;       // Su kontrolü için karakterin merkezinden aşağıya doğru olan mesafe.

    [Header("Ground & Slope Handling")]
    [SerializeField] private float groundCheckRadius = 0.2f;    // Karakterin altından ne kadar mesafede zemin kontrolü yapılacağını belirler.
    [SerializeField] private float groundCheckDistance = 0.1f;  // Zemin kontrolü için karakterin altına gönderilecek ışının uzunluğu.
    [SerializeField] private LayerMask groundLayer;             // Hangi katmanların "zemin" olarak kabul edileceğini tanımlar.
    [SerializeField] private float slopeStickForce = 100f;      // Karakterin eğimli yüzeylerde aşağı kaymasını önlemek için yere doğru uygulanan yapışma kuvveti.

    [Header("Custom Gravity")]
    [SerializeField] private float extraGravityForce = 20f;     // Zıplama ve düşme hissini daha tok ve gerçekçi kılmak için standart yerçekimine ek olarak uygulanan kuvvet.
    #endregion

    #region Input Değişkenleri

    private Vector2 moveInput;          // Oyuncunun hareket girdisini depolayan 2D vektör.
    private bool jumpInput = false;     // Oyuncunun zıplama tuşuna basıp basmadığını belirten boolean bayrak.
    private bool sprintInput = false;   // Oyuncunun sprint tuşuna basılı tutup tutmadığını belirten boolean bayrak.
    #endregion

    float lastStepTime = 0f;
    [SerializeField] private float stepSoundCooldown = 1.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        playerInputActions = new Input();

        playerInputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        playerInputActions.Player.Jump.performed += ctx => jumpInput = true;
        playerInputActions.Player.Jump.canceled += ctx => jumpInput = false;

        playerInputActions.Player.Run.performed += ctx => sprintInput = true;
        playerInputActions.Player.Run.canceled += ctx => sprintInput = false;

        // Cursor gizleme ve kilitleme
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() => playerInputActions.Player.Enable();      // Oyuncu input eylemlerini dinlemeye başlar.

    private void OnDisable() => playerInputActions.Player.Disable();    // Oyuncu input eylemlerini dinlemeyi durdurur.

    // Hareket ve fizik mantığı.
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

    // Karakter yerdeyken uygulanacak hareket mantığını yönetir.
    private void HandleGroundedMovement()
    {
        rb.linearDamping = dragOnGround;
        float currentSpeed = sprintInput ? moveSpeed * sprintSpeedMultipler : moveSpeed;
        rb.AddForce(GetMoveDirection() * currentSpeed, ForceMode.Acceleration);

        if (moveInput != Vector2.zero)
            CallStepSound(); // Yürüyüş sesi efektini çağırır.
    }

    // Karakter havadayken uygulanacak hareket mantığını yönetir.
    private void HandleAirMovement()
    {
        float currentSpeed = sprintInput ? moveSpeed * sprintSpeedMultipler : moveSpeed;
        rb.linearDamping = dragInAir;
        rb.AddForce(GetMoveDirection() * currentSpeed * airControlMultiplier, ForceMode.Acceleration);
    }

    // Karakter sudayken uygulanacak hareket mantığını yönetir.
    private void HandleSwimmingMovement()
    {
        rb.useGravity = false;
        rb.linearDamping = dragInWater;
        rb.AddForce(Vector3.up * buoyancy, ForceMode.VelocityChange);

        // Hareket yönünü, oyuncunun girdisine ve kameranın baktığı yöne göre hesaplar.
        Vector3 swimDirection = (playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x).normalized;
        // Belirlenen yönde yüzme kuvvetini uygular.
        rb.AddForce(swimDirection * swimForce, ForceMode.Acceleration);
    }

    // Zıplama girdisini oyuncunun mevcut durumuna göre işler.
    private void HandleJumpInput()
    {
        // Eğer karakter yerdeyse, anlık bir dikey kuvvet uygulayarak zıplamasını sağlar.
        if (currentState == PlayerState.Grounded && jumpInput)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        // Eğer karakter su içindeyse, zıplama tuşu yukarı yüzmek için kullanılır.
        else if (currentState == PlayerState.Swimming && jumpInput)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, swimAscendSpeed, rb.linearVelocity.z);
    }

    // Karakterin eğimli yüzeylerde kaymasını engellemek için ek bir kuvvet uygular.
    private void ApplySlopeStickingForce()
    {
        // Oyuncu hareket etmiyorsa bu kuvvete gerek yoktur.
        if (moveInput == Vector2.zero) return;

        // Karakterin altındaki zemini tespit etmek için aşağı doğru bir ışın gönderir.
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, (capsuleCollider.height / 2) + 0.5f, groundLayer))
        {
            // Eğer zemin düz değilse (eğimliyse).
            if (hit.normal != Vector3.up)
            {
                // Zeminin normal vektörünün tersi yönünde bir kuvvet uygulayarak karakteri yere yapıştırır.
                rb.AddForce(-hit.normal * slopeStickForce, ForceMode.Force);
            }
        }
    }

    // Havadayken daha hızlı düşmesini sağlamak için ek bir yerçekimi kuvveti uygular.
    private void ApplyExtraGravity()
    {
        // Bu, zıplama hissini daha az "havada süzülür" gibi yapar.
        rb.AddForce(Vector3.down * extraGravityForce, ForceMode.Acceleration);
    }

    // Oyuncu girdisine ve kamera açısına göre hedeflenen hareket yönünü hesaplar.
    private Vector3 GetMoveDirection()
    {
        // Kameranın ileri ve sağ yön vektörlerini alır.
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        // Y eksenindeki değerleri sıfırlar, böylece hareket sadece yatay düzlemde olur.
        camForward.y = 0;
        camRight.y = 0;
        // Girdi ve kamera yönlerini birleştirip normalize ederek son hareket yönü vektörünü oluşturur.
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }

    // Karakterin yatay hızını belirlenen maksimum değerle sınırlar.
    private void LimitVelocity()
    {
        // Karakterin yatay hızını tutar.
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (horizontalVelocity.magnitude > maxVelocity)
        {
            // Hız vektörünü, yönünü koruyarak maksimum hız büyüklüğüne indirger.
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxVelocity;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void ApplyUpdateFOV()
    {
        float targetFOV = sprintInput ? 80f : 60f;
        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * 5f);
    }

    private void CallStepSound()
    {
        if (Time.time - lastStepTime >= stepSoundCooldown)
        {
            playerAudioSource.PlayOneShot(walkSound);
            lastStepTime = Time.time;
        }
    }

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
}
