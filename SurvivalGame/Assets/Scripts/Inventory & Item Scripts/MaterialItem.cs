using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Material Item")]
public class MaterialItem : ItemData
{
    public override ItemType itemType => ItemType.Material;
}