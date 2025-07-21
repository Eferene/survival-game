using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public void DropItemButton(Slider slider)
    {
        PlayerInventory playerInventory = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>();
        if (slider.value > 0)
        {
            playerInventory.DropItem((int)slider.value);
        }
        else
        {
            playerInventory.OpenCloseDropUI();
        }
    }

    public void CloseUI(GameObject ui)
    {
        if (ui != null)
        {
            ui.SetActive(false);
        }
    }
}
