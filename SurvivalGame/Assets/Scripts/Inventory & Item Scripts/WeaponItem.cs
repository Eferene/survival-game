using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Weapon Item")]
public class WeaponItem : ItemData
{
    public int damage;
    public float attackSpeed;

    public override ItemType itemType => ItemType.Weapon;
}