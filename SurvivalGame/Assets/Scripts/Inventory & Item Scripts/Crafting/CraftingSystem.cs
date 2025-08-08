using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CraftingSystem : MonoBehaviour
{
    [SerializeField] GameObject itemsParent;
    [SerializeField] GameObject itemPrefab;

    [SerializeField] List<ItemData> craftableItems;

    public void LoadCraftableItems(CraftingType type)
    {
        craftableItems.Clear();
        foreach (Transform child in itemsParent.transform)
        {
            Destroy(child.gameObject);
        }

        UnityEngine.Object[] allObjects = Resources.LoadAll("Items", typeof(ScriptableObject));

        craftableItems = new List<ItemData>();

        foreach (UnityEngine.Object obj in allObjects)
        {
            if (obj is ItemData item)
            {
                if (item.isCraftable)
                {
                    if (type == CraftingType.Workbench)
                    {
                        if (item.craftingType == type || item.craftingType == CraftingType.Hand)
                        {
                            craftableItems.Add(item);
                            GameObject newUIItem = Instantiate(itemPrefab);
                            newUIItem.transform.SetParent(itemsParent.transform, false);
                            newUIItem.GetComponent<CraftableItemSlotUI>().item = item;
                        }
                    }
                    else
                    {
                        if (item.craftingType == type)
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
    }

}
