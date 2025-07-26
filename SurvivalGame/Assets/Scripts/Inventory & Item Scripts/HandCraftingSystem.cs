using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HandCraftingSystem : MonoBehaviour
{
    [SerializeField] GameObject itemsParent;
    [SerializeField] GameObject itemPrefab;

    [SerializeField] List<ItemData> craftableItems;
    void Start()
    {
        LoadCraftableItems();
    }

    public void LoadCraftableItems()
    {
        UnityEngine.Object[] allObjects = Resources.LoadAll("Items", typeof(ScriptableObject));

        craftableItems = new List<ItemData>();

        foreach (UnityEngine.Object obj in allObjects)
        {
            if (obj is ItemData item)
            {
                if (item.isCraftable && item.craftingType == CraftingType.Hand)
                {
                    craftableItems.Add(item);
                    GameObject newUIItem = Instantiate(itemPrefab);
                    newUIItem.transform.SetParent(itemsParent.transform, false);
                    newUIItem.GetComponent<CraftableItemSlotUI>().item = item;
                }
            }
        }
    }

}
