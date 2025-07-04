using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Slot UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI itemQuantityText;

    public void UpdateUI(ItemData itemData, int quantity)
    {
        if (itemData != null)
        {
            itemIcon.sprite = itemData.itemIcon;
            itemIcon.color = Color.white;
            itemQuantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(0, 0, 0, 0);
            itemQuantityText.text = string.Empty;
        }
    }
}
