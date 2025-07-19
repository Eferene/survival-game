using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    public ItemData handItem;
    public GameObject handItemGO;

    [Header("Player Inventory Settings")]
    private Input playerUIActions;
    [SerializeField] private GameObject inventoryPanelGO;
    public bool isHoldingKey;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private GameObject crosshair;

    [Header("Slots")]
    public InventorySlot[] inventorySlots = new InventorySlot[8];
    public InventorySlotUI[] inventorySlotUIs = new InventorySlotUI[8];

    [Header("Raycaster")]
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public Transform handTransform;
    private InputAction pointerPositionAction;

    private void Awake()
    {
        playerUIActions = new Input();

        pointerPositionAction = playerUIActions.UI.Point;
        pointerPositionAction.Enable();
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
            UseItem();
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
            if (inventorySlots[i].itemData == itemData)
            {
                if (itemData.isStackable == false) continue;
                if (itemData.maxStackSize == inventorySlots[i].quantity) continue;
                else if (itemData.maxStackSize < inventorySlots[i].quantity + quantity)
                {
                    quantity -= inventorySlots[i].quantity;
                    inventorySlots[i].quantity = itemData.maxStackSize;
                    inventorySlotUIs[i].UpdateUI(itemData, inventorySlots[i].quantity);
                    continue;
                }
                else if (itemData.maxStackSize >= inventorySlots[i].quantity + quantity)
                {
                    inventorySlots[i].quantity += quantity;
                    inventorySlotUIs[i].UpdateUI(itemData, inventorySlots[i].quantity);
                    return;
                }
            }
            else if (inventorySlots[i].itemData == null)
            {
                inventorySlots[i].itemData = itemData;
                if (quantity > itemData.maxStackSize)
                {
                    quantity -= itemData.maxStackSize;
                    inventorySlots[i].quantity = itemData.maxStackSize;
                    inventorySlotUIs[i].UpdateUI(itemData, inventorySlots[i].quantity);
                    continue;
                }
                else
                {
                    inventorySlots[i].quantity = quantity;
                    inventorySlotUIs[i].UpdateUI(itemData, inventorySlots[i].quantity);
                    return;
                }
            }
        }
    }

    public void UseItem()
    {
        Vector2 mousePos = pointerPositionAction.ReadValue<Vector2>();

        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = mousePos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            InventorySlotUI slotUI = result.gameObject.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                int index = System.Array.IndexOf(inventorySlotUIs, slotUI);
                if (index >= 0 && inventorySlots[index].itemData != null)
                {
                    if (handTransform.childCount > 0)
                    {
                        if (handTransform.GetChild(0).GetComponent<Object>().item != inventorySlots[index].itemData)
                        {
                            Destroy(handTransform.GetChild(0).gameObject);
                            ItemData itemData = inventorySlots[index].itemData;
                            handItem = itemData;
                            
                            GameObject newItem = Instantiate(itemData.itemPrefab, handTransform.position, Quaternion.identity, handTransform);
                            newItem.transform.localRotation = Quaternion.Euler(newItem.GetComponent<Object>().item.handRotation);
                            newItem.transform.localPosition = newItem.GetComponent<Object>().item.handPosition;
                            newItem.GetComponent<Object>().SetPhysicsEnabled(false);

                            handItemGO = newItem;
                        }
                    }
                    else if (handTransform.childCount == 0)
                    {
                        ItemData itemData = inventorySlots[index].itemData;
                        handItem = itemData;

                        GameObject newItem = Instantiate(itemData.itemPrefab, handTransform.position, Quaternion.identity, handTransform);
                        newItem.transform.localRotation = Quaternion.Euler(newItem.GetComponent<Object>().item.handRotation);
                        newItem.transform.localPosition = newItem.GetComponent<Object>().item.handPosition;
                        newItem.GetComponent<Object>().SetPhysicsEnabled(false);

                        handItemGO = newItem;
                    }
                }
            }
        }
    }
}
