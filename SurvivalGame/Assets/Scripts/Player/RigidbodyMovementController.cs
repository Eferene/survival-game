using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Bu component'in eklendiği GameObject'te Rigidbody ve CapsuleCollider bileşenlerinin bulunmasını zorunlu kılar.
// Bu, script'in doğru çalışması için gerekli olan temel fizik bileşenlerini garanti altına alır.
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
    #endregion

    #region Hareket Parametreleri (Inspector'dan Ayarlanabilir)
    [Header("Movement - Ground")]

    [SerializeField] private float moveSpeed = 8f;              // Karakterin yerdeki standart yürüme hızı.
    [SerializeField] private float sprintSpeed = 12f;           // Karakterin sprint durumundaki hızı.
    [SerializeField] private float maxVelocity = 15f;           // Karakterin ulaşabileceği maksimum yatay hız. Bu, hızın kontrolsüzce artmasını önler.
    [SerializeField] private float dragOnGround = 6f;           // Karakter yerdeyken uygulanan sürtünme kuvveti. Hareketi durdurduğunda yavaşça kaymasını engeller.

    [Header("Movement - Air/Jump")]
    [SerializeField] private float jumpForce = 8f;              // Zıplama anında karaktere uygulanan anlık dikey kuvvet.
    [SerializeField] private float airControlMultiplier = 0.5f; // Karakter havadayken hareket kontrolünün ne kadar etkili olacağını belirleyen çarpandır.
    [SerializeField] private float dragInAir = 0.5f;            // Karakter havadayken uygulanan sürtünme. Genellikle yerdeki sürtünmeden daha düşüktür.

    [Header("Movement - Swimming")]
    [SerializeField] private float swimForce = 5f;              // Karakter sudayken hareket etmek için uygulanan kuvvet.
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

    // Script'in ilk örneklendiği anda, oyun başlamadan önce çağrılır.
    // Temel component referanslarını ve input olaylarını ayarlamak için kullanılır.
    private void Awake()
    {
        // Gerekli component referanslarını alıp değişkenlere atar (caching).
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Yeni bir Input Actions nesnesi oluşturur.
        playerInputActions = new Input();

        // "Move" eylemi gerçekleştirildiğinde (tuşa basıldığında), moveInput değişkenini günceller.
        playerInputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        // "Move" eylemi iptal edildiğinde (tuş bırakıldığında), moveInput değişkenini sıfırlar.
        playerInputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // "Jump" eylemi gerçekleştirildiğinde zıplama mantığını tetikler.
        playerInputActions.Player.Jump.performed += ctx => HandleJumpInput();

        // "Run" eylemi gerçekleştirildiğinde sprint modunu aktif eder.
        playerInputActions.Player.Run.performed += ctx => sprintInput = true;
        // "Run" eylemi iptal edildiğinde sprint modunu deaktif eder.
        playerInputActions.Player.Run.canceled += ctx => sprintInput = false;

        // Cursor gizleme ve kilitleme işlemlerini yapar.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Bu script etkinleştirildiğinde çağrılır. Oyuncu input eylemlerini dinlemeye başlar.
    private void OnEnable() => playerInputActions.Player.Enable();

    // Bu script devre dışı bırakıldığında çağrılır. Oyuncu input eylemlerini dinlemeyi durdurur.
    private void OnDisable() => playerInputActions.Player.Disable();

    // Tüm hareket ve fizik mantığı burada işlenir.
    private void FixedUpdate()
    {
        // Her fizik adımında oyuncunun mevcut durumunu (yerde, havada, suda) kontrol eder ve günceller.
        UpdatePlayerState();

        // Mevcut duruma göre uygun hareket mantığını çalıştıran durum makinesi.
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

        // Her fizik adımının sonunda, karakterin hızının belirlenen maksimum limiti aşıp aşmadığını kontrol eder.
        LimitVelocity();
        // Ayrıca, karakterin hızına göre kamera FOV'sini günceller.
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

    // Karakterin su içinde olup olmadığını kontrol eder.
    private bool IsinWater()
    {
        // Karakterin merkezinde, "Water" katmanıyla temas eden bir küre olup olmadığını kontrol eder.
        return Physics.CheckSphere(transform.position + Vector3.down * swimmingCheckDistance, swimmingCheckRadius, LayerMask.GetMask("Water"), QueryTriggerInteraction.Collide);
    }

    // Karakterin zemine temas edip etmediğini kontrol eder.
    private bool IsGrounded()
    {
        // Karakterin kapsül collider'ının alt merkezinden biraz aşağıya bir pozisyon belirler.
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckDistance;
        // Belirlenen pozisyonda, "groundLayer" olarak işaretlenmiş katmanlarla temas eden bir küre olup olmadığını kontrol eder.
        return Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    // Karakter yerdeyken uygulanacak hareket mantığını yönetir.
    private void HandleGroundedMovement()
    {
        // Yerdeyken daha yüksek sürtünme uygular, bu da daha kontrollü duruş sağlar.
        rb.linearDamping = dragOnGround;
        // Sprint tuşuna basılıyorsa sprint hızını, basılmıyorsa normal yürüme hızını kullanır.
        float currentSpeed = sprintInput ? sprintSpeed : moveSpeed;
        // Hesaplanan hareket yönü ve hıza göre karaktere sürekli bir kuvvet uygular.
        rb.AddForce(GetMoveDirection() * currentSpeed, ForceMode.Force);
    }

    // Karakter havadayken uygulanacak hareket mantığını yönetir.
    private void HandleAirMovement()
    {
        // Havadayken daha düşük sürtünme uygular, bu da daha akıcı bir hareket sağlar.
        rb.linearDamping = dragInAir;
        // Havadaki kontrolü azaltmak için hareket kuvvetini airControlMultiplier ile çarparak uygular.
        rb.AddForce(GetMoveDirection() * moveSpeed * airControlMultiplier, ForceMode.Force);
    }

    // Karakter sudayken uygulanacak hareket mantığını yönetir.
    private void HandleSwimmingMovement()
    {
        // Sudayken Unity'nin standart yerçekimini devre dışı bırakır.
        rb.useGravity = false;
        // Suya özgü sürtünme uygular.
        rb.linearDamping = dragInWater;
        // Karakteri su yüzeyine doğru iten kaldırma kuvvetini uygular.
        rb.AddForce(Vector3.up * buoyancy, ForceMode.Force);

        // Hareket yönünü, oyuncunun girdisine ve kameranın baktığı yöne göre hesaplar.
        Vector3 swimDirection = (playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x).normalized;
        // Belirlenen yönde yüzme kuvvetini uygular.
        rb.AddForce(swimDirection * swimForce, ForceMode.Force);

        // Zıplama tuşuna basılıyorsa, karakteri su içinde yukarı doğru iter.
        if (jumpInput)
        {
            rb.AddForce(Vector3.up * swimForce, ForceMode.Force);
        }
    }

    // Zıplama girdisini oyuncunun mevcut durumuna göre işler.
    private void HandleJumpInput()
    {
        // Eğer karakter yerdeyse, anlık bir dikey kuvvet uygulayarak zıplamasını sağlar.
        if (currentState == PlayerState.Grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        // Eğer karakter su içindeyse, zıplama tuşu yukarı yüzmek için kullanılır.
        else if (currentState == PlayerState.Swimming)
        {
            // Sürekli yukarı çıkmayı önlemek için zıplama girdisini bir bayrakla yönetir.
            jumpInput = true;
            // Kısa bir süre sonra bu bayrağı sıfırlayarak tekrar basılmasını bekler.
            Invoke(nameof(ResetJumpInput), 0.2f);
        }
    }

    // Suda yukarı yüzmek için kullanılan zıplama girdisi bayrağını sıfırlar.
    private void ResetJumpInput() => jumpInput = false;

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
        // Y eksenindeki değerleri sıfırlar, böylece hareket sadece yatay düzlemde (XZ) olur.
        camForward.y = 0;
        camRight.y = 0;
        // Girdi ve kamera yönlerini birleştirip normalize ederek son hareket yönü vektörünü oluşturur.
        return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
    }

    // Karakterin yatay hızını belirlenen maksimum değerle sınırlar.
    private void LimitVelocity()
    {
        // Rigidbody'nin mevcut hızının sadece yatay bileşenlerini (x ve z) alır.
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Eğer yatay hızın büyüklüğü maksimum hızı aşıyorsa...
        if (horizontalVelocity.magnitude > maxVelocity)
        {
            // Hız vektörünü, yönünü koruyarak maksimum hız büyüklüğüne indirger.
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxVelocity;
            // Rigidbody'nin hızını, sınırlanmış yatay hız ve orijinal dikey hız ile günceller.
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void ApplyUpdateFOV()
    {
        float targetFOV = sprintInput ? 80f : 60f; // Sprint yapılıyorsa FOV'yi artır, değilse normalde tut.
        playerCamera.Lens.FieldOfView = Mathf.Lerp(playerCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * 5f);
    }

    // Unity Editor'da çalışırken, seçili olan bu objenin etrafına yardımcı görseller (Gizmo) çizer.
    private void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        Gizmos.color = Color.yellow;
        // IsGrounded metodunda kullanılan kürenin konumunu ve boyutunu editörde görselleştirir.
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);

        Gizmos.color = Color.blue;
        // IsinWater metodunda kullanılan kürenin konumunu ve boyutunu editörde görselleştirir.
        Vector3 spherePosition2 = transform.position + Vector3.down * swimmingCheckDistance;
        Gizmos.DrawWireSphere(spherePosition2, swimmingCheckRadius);
    }
}
