using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Clothing Item")]
public class ClothingItem : ItemData
{
    public float protectionValue;

    public override ItemType itemType => ItemType.Clothing;
}