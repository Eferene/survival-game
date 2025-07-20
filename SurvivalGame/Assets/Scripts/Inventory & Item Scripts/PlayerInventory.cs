using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    public ItemData handItem;
    public GameObject handItemGO;
    public int selectedSlotIndex = -1; // -1 hiç bir item seçilmediğini gösterir

    [Header("Player Inventory Settings")]
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject inventoryPanelGO;
    private Input playerActions;
    public bool isHoldingKey;

    [Header("Slots")]
    public InventorySlot[] inventorySlots = new InventorySlot[8];
    public InventorySlotUI[] inventorySlotUIs = new InventorySlotUI[8];

    [Header("Raycaster")]
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;
    public Transform handTransform;
    private InputAction pointerPositionAction;

    [Header("UI Elements")]
    [SerializeField] private GameObject dropItemUI;
    [SerializeField] private Slider dropItemSlider;
    [SerializeField] private TextMeshProUGUI dropItemText;
    private bool isDropUIOpen = false;
    bool isInventoryOpen = false;

    private void Awake()
    {
        playerActions = new Input();

        pointerPositionAction = playerActions.UI.Point;
        pointerPositionAction.Enable();
    }

    private void OnEnable()
    {
        playerActions.Player.Enable();
        playerActions.Player.Drop.performed += ctx => OpenCloseDropUI();

        playerActions.UI.Enable();
        playerActions.UI.Inventory.performed += ctx => OpenInventory();
        playerActions.UI.Inventory.canceled += ctx => CloseInventory();
    }

    private void OnDisable()
    {
        playerActions.Player.Disable();
        playerActions.Player.Drop.performed -= ctx => OpenCloseDropUI();

        playerActions.UI.Disable();
        playerActions.UI.Inventory.performed -= ctx => OpenInventory();
        playerActions.UI.Inventory.canceled -= ctx => CloseInventory();
    }

    private void Update()
    {
        if (isInventoryOpen) UseItem();
        if (isDropUIOpen && handItemGO != null)
        {
            dropItemText.text = dropItemSlider.value + "/" + handItemGO.GetComponent<Object>().quantity;
        }
    }

    private void OpenInventory()
    {
        inventoryPanelGO.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerCamera.GetComponent<CinemachineInputAxisController>().enabled = false;
        crosshair.SetActive(false);
        isInventoryOpen = true;
    }

    public void CloseInventory()
    {
        inventoryPanelGO.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerCamera.GetComponent<CinemachineInputAxisController>().enabled = true;
        crosshair.SetActive(true);
        isInventoryOpen = false;
    }

    public void OpenCloseDropUI()
    {
        if (dropItemUI.activeSelf)
        {
            dropItemUI.SetActive(false);
            isDropUIOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = true;
        }
        else if(handItemGO != null && !dropItemUI.activeSelf)
        {
            dropItemUI.SetActive(true);
            isDropUIOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = false;
            dropItemSlider.maxValue = handItemGO.GetComponent<Object>().quantity;
            dropItemSlider.value = 0;
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
                    selectedSlotIndex = index;
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
                            newItem.GetComponent<Object>().quantity = inventorySlots[index].quantity;
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
                        newItem.GetComponent<Object>().quantity = inventorySlots[index].quantity;
                        newItem.GetComponent<Object>().SetPhysicsEnabled(false);

                        handItemGO = newItem;
                    }
                }
            }
        }
    }
}
