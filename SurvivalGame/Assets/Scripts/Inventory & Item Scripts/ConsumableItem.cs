using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Consumable Item")]
public class ConsumableItem : ItemData
{
    public float healthRestore;
    public float staminaRestore;
    public float hungerRestore;
    public float thirstRestore;

    public override ItemType itemType => ItemType.Consumable;
}