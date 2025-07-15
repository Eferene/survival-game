using UnityEngine;

public class Breakable : MonoBehaviour
{
    public int hp;
    public GameObject ore;
    public int minDrop;
    public int maxDrop;

    public void Update()
    {
        if (hp <= 0)
        {
            GameObject newOre = Instantiate(ore, transform.position, Quaternion.identity);
            newOre.GetComponent<Object>().quantity = Random.Range(minDrop, maxDrop);
            Destroy(gameObject);
        }
    }
}
