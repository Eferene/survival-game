using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;

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

    private float lastHitTime = -1f;
    private float hitCooldown = 0.5f;

    private float targetFOV = 60f; // Hedef FOV değeri
    [SerializeField] private float transitionSpeed = 5f; // FOV geçiş hızı

    [Header("Raycast & Inventory Settings")]
    public PlayerInventory playerInventory;
    public GameObject damageTextPrefab; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInventory = GetComponent<PlayerInventory>();
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

        #region Animator & Animations
        if (playerInventory.handItemGO != null)
        {
            if (playerInventory.handItemGO.GetComponent<Animator>() != null)
            {
                if(playerInputActions.Player.Hit.triggered)
                {
                    if(Time.time - lastHitTime > hitCooldown)
                    {
                        playerInventory.handItemGO.GetComponent<Animator>().SetTrigger("Hit");
                    }
                }
            }
        }
        #endregion
        //Raycast
        RaycastHit hit;
        #region Item Alma Raycast
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, Mathf.Infinity))
        {
            if (hit.distance < 5)
            {
                if (hit.collider.CompareTag("Item") || hit.collider.CompareTag("Consumable"))
                {
                    if (playerInputActions.Player.Interaction.triggered)
                    {
                        if (hit.collider.gameObject.GetComponent<Object>().item != null)
                        {
                            playerInventory.AddItemToInventory(hit.collider.gameObject.GetComponent<Object>().item, hit.collider.gameObject.GetComponent<Object>().quantity);
                            Destroy(hit.collider.gameObject);
                        }
                    }
                }
            }
        }
        #endregion
        #region Hit Raycast
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, Mathf.Infinity))
        {
            if (hit.distance < 3)
            {
                if (playerInventory.handItem != null && playerInventory.handItem.itemType == ItemType.Tool)
                {
                    ToolItem toolItem = playerInventory.handItem as ToolItem;
                    for (int i = 0; i < toolItem.effectiveTags.Length; i++)
                    {
                        if (hit.collider.CompareTag(toolItem.effectiveTags[i]))
                        {
                            if (playerInputActions.Player.Hit.triggered && Time.time - lastHitTime > hitCooldown)
                            {
                                if (hit.collider.gameObject.GetComponent<Breakable>() != null)
                                {
                                    Breakable breakable = hit.collider.gameObject.GetComponent<Breakable>();
                                    int dmg = Random.Range(toolItem.minEfficiency, toolItem.maxEfficiency + 1);
                                    breakable.TakeDamage(dmg);
                                    lastHitTime = Time.time;
                                    GameObject damageText = Instantiate(damageTextPrefab, hit.point, Quaternion.identity);
                                    damageText.GetComponent<TextMeshPro>().text = dmg.ToString();
                                    damageText.transform.localScale = Vector3.one * 0.2f; // Sabit, her objede aynı büyüklükte
                                    damageText.transform.DOMoveY(damageText.transform.position.y + 1f, 1f).OnComplete(() => Destroy(damageText));
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }

    private float lastSprintTime;
    private void FixedUpdate()
    {
        float currentSpeed = moveSpeed; // Varsayılan hız olarak normal hareket hızını kullan
        if (isSprinting && playerGeneral.CurrentStamina > 0)
        {
            currentSpeed = sprintSpeed; // Sprint hızını kullan
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
