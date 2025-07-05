using Unity.Cinemachine;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Player Inventory Settings")]
    private Input playerUIActions;
    [SerializeField] private GameObject inventoryPanelGO;
    public bool isHoldingKey;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private GameObject crosshair;

    [Header("Slots")]
    public InventorySlot[] inventorySlots = new InventorySlot[8];
    public InventorySlotUI[] inventorySlotUIs = new InventorySlotUI[8];

    private void Awake()
    {
        playerUIActions = new Input();
    }

    private void OnEnable()
    {
        playerUIActions.UI.Enable();
        playerUIActions.UI.Inventory.performed += ctx => isHoldingKey = true;
        playerUIActions.UI.Inventory.canceled += ctx => isHoldingKey = false;
    }

    private void OnDisable()
    {
        playerUIActions.UI.Disable();
        playerUIActions.UI.Inventory.performed -= ctx => isHoldingKey = true;
        playerUIActions.UI.Inventory.canceled -= ctx => isHoldingKey = false;
    }

    private void Update()
    {
        if (isHoldingKey)
        {
            inventoryPanelGO.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = false;
            crosshair.SetActive(false);
        }
        else
        {
            inventoryPanelGO.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = true;
            crosshair.SetActive(true);
        }
    }

    public void AddItemToInventory(ItemData itemData, int quantity)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].itemData == null)
            {
                inventorySlots[i].itemData = itemData;
                inventorySlots[i].quantity = quantity;
                inventorySlotUIs[i].UpdateUI(itemData, quantity);
                return;
            }
            else if (inventorySlots[i].itemData == itemData)
            {
                if (itemData.isStackable == false) { return; }
                if (itemData.maxStackSize <= inventorySlots[i].quantity + quantity)
                {
                    inventorySlots[i].quantity = itemData.maxStackSize;
                    quantity = itemData.maxStackSize - inventorySlots[i].quantity;
                    return;
                }
                inventorySlots[i].quantity += quantity;
                inventorySlotUIs[i].UpdateUI(itemData, inventorySlots[i].quantity);
                return;
            }
        }
    }
}
