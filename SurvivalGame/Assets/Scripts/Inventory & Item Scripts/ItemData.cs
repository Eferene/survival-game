using NaughtyAttributes;
using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public int itemID;
    public string itemName;
    [TextArea] public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public bool isStackable;
    [ShowIf("isStackable")] public int maxStackSize;
    public Vector3 handPosition;
    public Vector3 handRotation;
    public bool isCraftable;

    [ShowIf("isCraftable")] public CraftingType craftingType;
    [ShowIf("isCraftable")] public CraftingData[] crafting;

    public abstract ItemType itemType { get; }
}

[System.Serializable]
public class CraftingData
{
    public ItemData craftingMaterial;
    public int craftingMaterialAmount;
}

public enum ItemType
{
    Material,
    Tool,
    Weapon,
    Consumable,
    Clothing
}

public enum CraftingType
{
    Hand,
    Workbench
}