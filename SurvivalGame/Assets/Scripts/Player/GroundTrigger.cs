using JetBrains.Annotations;
using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    [Header("Ground Check Ayarları")]
    [Tooltip("Karakterin altındaki zemini kontrol edecek kürenin yarıçapı.")]
    public float sphereRadius = 0.3f;

    [Tooltip("Zemini ne kadar aşağıda arasın?")]
    public float groundCheckDistance = 0.1f;

    [Tooltip("Hangi layer'ın zemin olduğunu belirtir.")]
    public LayerMask groundLayer;

    // Bu değişkenler dışarıdan okunabilir ama bu script'ten değiştirilir.
    public bool isGrounded;

    private RaycastHit _hitInfo; // Vuruş bilgilerini burada saklanacak.

    public RaycastHit HitInfo => _hitInfo; // Dışarıdan erişim için.

    [SerializeField] private float maxSlopeAngle = 45f;

    void FixedUpdate()
    {
        CheckGround();
    }

    private void CheckGround()
    {
        // Karakterin pozisyonundan biraz aşağıdan başlayarak aşağı doğru bir küre "ateşliyoruz".
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        // Physics.SphereCast bize true/false döner. Eğer bir şeye çarparsa true olur.
        isGrounded = Physics.SphereCast(origin, sphereRadius, Vector3.down, out _hitInfo, groundCheckDistance, groundLayer);
    }

    // Bu fonksiyon editörde Scene ekranında bize görsel bir küre çizer. Ayar yaparken hayat kurtarır.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(origin + Vector3.down * (isGrounded ? _hitInfo.distance : groundCheckDistance), sphereRadius);
    }

    public bool OnSlope()
    {
        // Eğer zemin kontrolü yapıldıysa ve zemin var ise, eğim kontrolü yapar.
        if (isGrounded && _hitInfo.normal != Vector3.up)
        {
            float angle = Vector3.Angle(_hitInfo.normal, Vector3.up);
            return angle > 0 && angle < maxSlopeAngle; // 45 derece eğimden daha az ise eğimli yüzeydeyiz.
        }
        return false; // Zemin yoksa veya düz ise false döner.
    }
}
