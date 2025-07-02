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
    [SerializeField] float fallMultiplier = 5f;

    [Header("Camera & Look Settings")]
    [SerializeField] Transform cameraTransform;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Script'in ilk kez aktif olduğu anda çalışır.
    private void OnDisable()
    {
        // Player action map'ini devre dışı bırak.
        // Oyun durunca veya karakter ölünce kaynakları serbest bırakmak amacıyla.
        playerInputActions.Player.Disable();
    }

    private void Update()
    {
        // Move eyleminden gelen Vector2 değerini okur ve moveInput değişkenine atar.
        moveInput = playerInputActions.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
    }

    private void LateUpdate()
    {
        // Kameranin ileri yonunu al
        Vector3 camForward = cameraTransform.forward;

        // Yukari-asagi bileeni sifirla, sadece yatay dondurme yap
        camForward.y = 0f;
        camForward.Normalize();

        // Eğer kamera bir yere bakiyorsa karakteri kameranin baktigi yone dogru dondur
        if (camForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward);
            transform.rotation = targetRotation;
        }
    }
}
