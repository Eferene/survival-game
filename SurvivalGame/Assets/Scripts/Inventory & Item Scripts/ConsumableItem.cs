using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Consumable Item")]
public class ConsumableItem : ItemData
{
    public float healthRestored;
    public float hungerRestored;
    public float thirstRestored;

    public override ItemType itemType => ItemType.Consumable;
}