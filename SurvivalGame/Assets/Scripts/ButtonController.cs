using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public void DropItemButton(Slider slider)
    {
        if (slider.value > 0)
        {
            PlayerInventory playerInventory = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>();
            playerInventory.handItemGO.GetComponent<Object>().quantity -= (int)slider.value;
            playerInventory.inventorySlots[playerInventory.selectedSlotIndex].quantity -= (int)slider.value;
            GameObject dropPrefab = playerInventory.inventorySlots[playerInventory.selectedSlotIndex].itemData.itemPrefab;

            if (playerInventory.handItemGO.GetComponent<Object>().quantity <= 0)
            {
                Destroy(playerInventory.handItemGO);
                playerInventory.handItemGO = null;
                playerInventory.handItem = null;
                playerInventory.inventorySlots[playerInventory.selectedSlotIndex].itemData = null;
                playerInventory.inventorySlotUIs[playerInventory.selectedSlotIndex].UpdateUI(null, 0);
                playerInventory.selectedSlotIndex = -1;
            }
            else
            {
                playerInventory.inventorySlotUIs[playerInventory.selectedSlotIndex].UpdateUI(playerInventory.handItem, playerInventory.handItemGO.GetComponent<Object>().quantity);
            }


            GameObject droppedItem = Instantiate(dropPrefab, playerInventory.handTransform.position, Quaternion.identity);
            droppedItem.GetComponent<Object>().quantity = (int)slider.value;
            droppedItem.GetComponent<Object>().SetPhysicsEnabled(true);
            playerInventory.OpenCloseDropUI();
        }
        else
        {
            PlayerInventory playerInventory = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>();
            playerInventory.OpenCloseDropUI();
        }
    }
}
