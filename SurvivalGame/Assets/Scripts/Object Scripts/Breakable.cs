using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

public class Breakable : MonoBehaviour
{
    public float hp;
    public Drop[] drops;
    public bool hasParent;

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (!hasParent)
        {
            transform.DOShakePosition(0.1f, 0.2f, 10, 90, false, true);
            transform.DOScale(transform.localScale * 1.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
            BreakObject(gameObject);
        }
        else
        {
            transform.parent.DOShakePosition(0.1f, 0.2f, 10, 90, false, true);
            transform.parent.DOScale(transform.parent.localScale * 1.1f, 0.1f).SetLoops(2, LoopType.Yoyo);
            BreakObject(transform.parent.gameObject);
        }
    }

    public void BreakObject(GameObject obj)
    {
        if (hp <= 0)
        {
            for (int j = 0; j < drops.Length; j++)
            {
                GameObject drop = drops[j].drop;
                int dropCount = Random.Range(drops[j].minDrop, drops[j].maxDrop + 1);

                Object dropObj = drop.GetComponent<Object>();
                int maxStack = dropObj.item.maxStackSize;

                if (!dropObj.item.isStackable)
                {
                    for (int i = 0; i < dropCount; i++)
                    {
                        Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 1, Random.Range(-0.5f, 0.5f));
                        Vector3 spawnPos = transform.position + randomOffset;

                        GameObject newDrop = Instantiate(drops[j].drop, spawnPos, Quaternion.identity);
                        newDrop.GetComponent<Object>().quantity = 1;
                        newDrop.GetComponent<Object>().SetPhysicsEnabled(true);
                    }
                }
                else
                {
                    while (dropCount > 0)
                    {
                        if (dropCount <= maxStack)
                        {
                            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 1, Random.Range(-0.5f, 0.5f));
                            Vector3 spawnPos = transform.position + randomOffset;

                            GameObject newDrop = Instantiate(drops[j].drop, spawnPos, Quaternion.identity);
                            newDrop.GetComponent<Object>().quantity = dropCount;
                            newDrop.GetComponent<Object>().SetPhysicsEnabled(true);
                            break;
                        }
                        else
                        {
                            GameObject newDrop = Instantiate(drops[j].drop, transform.position, Quaternion.identity);
                            newDrop.GetComponent<Object>().quantity = maxStack;
                            newDrop.GetComponent<Object>().SetPhysicsEnabled(true);
                            dropCount -= maxStack;
                        }
                    }
                }
            }
            DOTween.Kill(obj.transform);
            Destroy(obj);
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
