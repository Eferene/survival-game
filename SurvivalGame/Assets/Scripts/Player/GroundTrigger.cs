using JetBrains.Annotations;
using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    [Header("Ground Check Settings")]
    [Tooltip("Yere temas kontrolü için kullanılacak sphere'in yarıçapı.")]
    [SerializeField] private float sphereRadius = 0.3f;

    [Tooltip("Kontrol sphere'i ne kadar aşağıda konumlanacak.")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Tooltip("Zemin olarak kabul edilecek layer'lar.")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Maksimum eğim açısı (derece). Bu açının üstündeki yüzeylerde kayma olabilir.")]
    [SerializeField] private float maxSlopeAngle = 45f;

    [SerializeField] private PlayerController playerController;

    // Yere temas raycast sonucu (sadece okunabilir)
    private RaycastHit _hitInfo;
    public RaycastHit HitInfo => _hitInfo;


    void FixedUpdate()
    {
        CheckGround();
    }

    private void CheckGround()
    {
        // Karakterin pozisyonundan biraz aşağıdan başlayarak aşağı doğru bir küre "ateşliyoruz".
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        // origin: Kürenin başladığı nokta.
        // radius: Kürenin yarıçapı..
        // direction: Kürenin gidiş yönü.
        // hit: Çarpılan nesnenin bilgilerini döndürür. 'out' olarak kullanılır.
        // maxDistance: Sphere'in ne kadar uzağa gideceğini belirtir. Bu, zemin kontrol mesafesidir.
        // layerMask: Hangi layer’lara çarpacağını belirtir.
        playerController.isGrounded = Physics.SphereCast(origin, sphereRadius, Vector3.down, out _hitInfo, groundCheckDistance, groundLayer);
    }

    // Bu fonksiyon editörde Scene ekranında bize görsel bir küre çizer.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        // Kürenin merkezini hesaplıyoruz:
        // Eğer isGrounded true ise, küre çarpmanın gerçekleştiği mesafeye çizilir (_hitInfo.distance kadar aşağıda)
        // Değilse, yani temas yoksa, küre maksimum kontrol mesafesinde (groundCheckDistance kadar aşağıda) çizilir
        Gizmos.DrawWireSphere(origin + Vector3.down * (playerController.isGrounded ? _hitInfo.distance : groundCheckDistance), sphereRadius);
    }

    public bool OnSlope()
    {
        // Eğer zemin kontrolü yapıldıysa ve zemin var ise, eğim kontrolü yapar.
        if (playerController.isGrounded && _hitInfo.normal != Vector3.up)
        {
            float angle = Vector3.Angle(_hitInfo.normal, Vector3.up);
            return angle > 0 && angle < maxSlopeAngle; // 45 derece eğimden daha az ise eğimli yüzeydeyiz.
        }
        return false; // Zemin yoksa veya düz ise false döner.
    }
}
