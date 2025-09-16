using UnityEngine;

[ExecuteInEditMode]
public class BuildSnapPoint : MonoBehaviour
{
    public Vector3[] snapPoints;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3[] array = snapPoints;

        foreach (Vector3 vector in array)
        {
            Gizmos.DrawSphere(transform.position + vector * 1f, 0.25f);
        }
    }
}
