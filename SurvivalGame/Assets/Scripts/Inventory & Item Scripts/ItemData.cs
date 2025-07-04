using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public int itemID;
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public bool isStackable;
    public int maxStackSize;

    public abstract ItemType itemType { get; }
}

public enum ItemType
{
    Material,
    Tool,
    Weapon,
    Consumable,
    Clothing
}
