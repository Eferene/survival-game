using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

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

    public void CloseDropUI()
    {
        PlayerInventory playerInventory = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInventory>();
        playerInventory.OpenCloseDropUI();
    }

    public void CloseUI(GameObject ui)
    {
        if (ui != null)
        {
            GameObject fpscam = GameObject.FindGameObjectWithTag("FPSCam");
            fpscam.GetComponent<CinemachineInputAxisController>().enabled = true;
            ui.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
