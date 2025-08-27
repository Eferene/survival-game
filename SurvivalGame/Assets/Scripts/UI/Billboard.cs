using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Awake()
    {
        GetComponent<MeshRenderer>().sortingLayerName = "UI";
        GetComponent<MeshRenderer>().sortingOrder = 15;
    }

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
