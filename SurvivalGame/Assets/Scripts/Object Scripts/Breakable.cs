using UnityEngine;

public class Breakable : MonoBehaviour
{
    public float hp;
    public Drop[] drops;

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            for (int j = 0; j < drops.Length; j++)
            {
                GameObject newDrop = Instantiate(drops[j].drop, transform.position, Quaternion.identity);
                newDrop.GetComponent<Object>().quantity = Random.Range(drops[j].minDrop, drops[j].maxDrop + 1);
                newDrop.GetComponent<Object>().SetPhysicsEnabled(true);
            }
            Destroy(gameObject);
        }
    }
}

[System.Serializable]
public class Drop
{
    public GameObject drop;
    public int minDrop = 1;
    public int maxDrop = 5;
}
