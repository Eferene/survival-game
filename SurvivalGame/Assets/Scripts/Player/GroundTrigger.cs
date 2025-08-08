using JetBrains.Annotations;
using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    [Header("Ground Check Settings")]
    [Tooltip("Yere temas kontrolü için kullanılacak sphere'in yarıçapı.")]
    [SerializeField] private float sphereRadius = 0.3f;

    [Tooltip("Kontrol sphere'inin başlangıç noktasından ne kadar aşağıda olacağı.")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Tooltip("Zemin olarak kabul edilecek layer'lar.")]
    [SerializeField] private LayerMask groundLayer;

    // Güncel zeminin eğim açısı (derece cinsinden)
    public float slopeAngle;

    // Zeminin eğimli olup olmadığını tutar (sadece dahili kullanım)
    private bool isOnSlope;

    // PlayerController referansı, zemin temas bilgisini buraya iletmek için kullanılır
    [SerializeField] private PlayerController playerController;

    // SphereCast sonucu çarpılan yüzey bilgisi (sadece okunabilir)
    private RaycastHit groundHitInfo;
    public RaycastHit HitInfo => groundHitInfo;


    void FixedUpdate()
    {
        PerformGroundCheck();

        isOnSlope = CheckIfOnSlope();

        Debug.Log(CheckIfOnSlope() ? "Eğimli yüzeydeyiz." : "Düz yüzeydeyiz.");
    }

    /// <summary>
    /// Karakterin hemen altında yere temas edip etmediğini sphere cast ile kontrol eder.
    /// SphereCast, kapsül gibi etrafı kontrol ederek daha sağlam temas algılar.
    /// </summary>
    private void PerformGroundCheck()
    {
        Vector3 sphereCastOrigin = transform.position + Vector3.up * 0.1f;

        // sphereCastOrigin: Kürenin başladığı nokta.
        // sphereRadius: Kürenin yarıçapı..
        // Vector3.down: Kürenin gidiş yönü.
        // out groundHitInfo: Çarpılan nesnenin bilgilerini döndürür. 'out' olarak kullanılır.
        // groundCheckDistance: Sphere'in ne kadar uzağa gideceğini belirtir. Bu, zemin kontrol mesafesidir.
        // groundLayer: Hangi layer’lara çarpacağını belirtir.
        playerController.isGrounded = Physics.SphereCast(
            sphereCastOrigin,
            sphereRadius,
            Vector3.down,
            out groundHitInfo,
            groundCheckDistance,
            groundLayer
        );
    }

    /// <summary>
    /// Editör Scene view'da zemin kontrol sphere'ini görsel olarak çizer.
    /// Eğer zemine temas varsa, küre çarpmanın gerçekleştiği mesafeye çizilir, yoksa maksimum kontrol mesafesine.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        float drawDistance = playerController != null && playerController.isGrounded ? groundHitInfo.distance : groundCheckDistance;
        Gizmos.DrawWireSphere(origin + Vector3.down * drawDistance, sphereRadius);
    }

    /// <summary>
    /// Zeminin eğimli olup olmadığını kontrol eder.
    /// Eğer zemin varsa ve eğim normali yukarıya tam paralel değilse,
    /// eğim açısını hesaplar ve 0°'den büyükse true döner.
    /// </summary>
    /// <returns>True ise eğimli yüzeyde, false ise düz zemindeyiz.</returns>
    public bool CheckIfOnSlope()
    {
        if (playerController.isGrounded && groundHitInfo.normal != Vector3.up)
        {
            slopeAngle = Vector3.Angle(groundHitInfo.normal, Vector3.up);
            return slopeAngle > 0;
        }
        slopeAngle = 0f;
        return false;
    }
}
