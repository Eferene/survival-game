using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftableItemSlotUI : MonoBehaviour
{
    public ItemData item;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private GameObject requiredItemPanel;
    [SerializeField] private GameObject requiredItemPrefab;
    List<ItemData> requiredItems = new List<ItemData>();
    List<GameObject> requiredItemObjects = new List<GameObject>();
    PlayerInventory playerInventory;
    void Start()
    {
        playerInventory = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>();
        itemIcon.sprite = item.itemIcon;
        itemName.text = item.itemName;

        //Required Items
        if (item.crafting != null && item.crafting.Length > 0)
        {
            requiredItemPanel.SetActive(true);
            foreach (CraftingData craftingData in item.crafting)
            {
                GameObject newRequiredItem = Instantiate(requiredItemPrefab, requiredItemPanel.transform);
                newRequiredItem.GetComponentInChildren<Image>().sprite = craftingData.craftingMaterial.itemIcon;
                newRequiredItem.GetComponentInChildren<TextMeshProUGUI>().text = craftingData.craftingMaterialAmount.ToString();
                requiredItems.Add(craftingData.craftingMaterial);
                requiredItemObjects.Add(newRequiredItem);
            }
        }
        else
        {
            requiredItemPanel.SetActive(false);
        }
    }

    void Update()
    {
        //Required Item Check
        if (item.crafting != null && item.crafting.Length > 0)
        {
            for (int i = 0; i < requiredItems.Count; i++)
            {
                ItemData requiredItem = requiredItems[i];
                int requiredAmount = item.crafting[i].craftingMaterialAmount;
                int playerItemCount = 0;

                foreach (InventorySlot slot in playerInventory.inventorySlots)
                {
                    if (slot.itemData == requiredItem)
                    {
                        playerItemCount += slot.quantity;
                        requiredItemObjects[i].GetComponentInChildren<TextMeshProUGUI>().text = $"{playerItemCount}/{requiredAmount}";
                        UpdateRequiredItemColor(playerItemCount, requiredAmount, i);
                    }
                }

                if(playerItemCount < requiredAmount)
                {
                    requiredItemObjects[i].GetComponentInChildren<TextMeshProUGUI>().text = $"{playerItemCount}/{requiredAmount}";
                    UpdateRequiredItemColor(playerItemCount, requiredAmount, i);
                }
            }
        }
    }

    private void UpdateRequiredItemColor(int playerItemCount, int requiredAmount, int index)
    {
        TextMeshProUGUI textComponent = requiredItemObjects[index].GetComponentInChildren<TextMeshProUGUI>();
        if (playerItemCount >= requiredAmount)
        {
            textComponent.color = Color.green;
        }
        else
        {
            textComponent.color = Color.red;
        }
    }
}
