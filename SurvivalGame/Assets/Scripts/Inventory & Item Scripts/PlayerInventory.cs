using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    private PlayerGeneral playerGeneral;
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

    private void Start()
    {
        playerGeneral = GetComponent<PlayerGeneral>();
    }

    private void OnEnable()
    {
        playerActions.Player.Enable();
        playerActions.Player.Drop.performed += ctx => DropUIControl();

        playerActions.UI.Enable();
        playerActions.UI.Inventory.performed += ctx => OpenInventory();
        playerActions.UI.Inventory.canceled += ctx => CloseInventory();
    }

    private void OnDisable()
    {
        playerActions.Player.Disable();
        playerActions.Player.Drop.performed -= ctx => DropUIControl();

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

        if(isDropUIOpen || isInventoryOpen) playerGeneral.canHit = false;
        else playerGeneral.canHit = true;
    }

    private void OpenInventory()
    {
        if (!isDropUIOpen)
        {
            inventoryPanelGO.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = false;
            crosshair.SetActive(false);
            isInventoryOpen = true;
        }
    }

    public void CloseInventory()
    {
        if (!isDropUIOpen)
        {
            inventoryPanelGO.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = true;
            crosshair.SetActive(true);
            isInventoryOpen = false;
        }
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
        else if (handItemGO != null && !dropItemUI.activeSelf)
        {
            if (inventoryPanelGO.activeSelf) CloseInventory();

            dropItemUI.SetActive(true);
            isDropUIOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerCamera.GetComponent<CinemachineInputAxisController>().enabled = false;
            dropItemSlider.maxValue = handItemGO.GetComponent<Object>().quantity;
            dropItemSlider.value = 0;
        }
    }

    public void DropUIControl()
    {
        if (handItemGO != null)
        {
            if (handItemGO.GetComponent<Object>().quantity > 1)
            {
                OpenCloseDropUI();
            }
            else if (handItemGO.GetComponent<Object>().quantity == 1)
            {
                DropItem(1);
            }
        }
    }

    public void DropItem(int quant)
    {
        handItemGO.GetComponent<Object>().quantity -= quant;
        inventorySlots[selectedSlotIndex].quantity -= quant;
        GameObject dropPrefab = inventorySlots[selectedSlotIndex].itemData.itemPrefab;

        if (handItemGO.GetComponent<Object>().quantity <= 0)
        {
            Destroy(handItemGO);
            handItemGO = null;
            handItem = null;
            inventorySlots[selectedSlotIndex].itemData = null;
            inventorySlotUIs[selectedSlotIndex].UpdateUI(null, 0);
            selectedSlotIndex = -1;
        }
        else
        {
            inventorySlotUIs[selectedSlotIndex].UpdateUI(handItem, handItemGO.GetComponent<Object>().quantity);
        }

        GameObject droppedItem = Instantiate(dropPrefab, handTransform.position, Quaternion.identity);
        droppedItem.GetComponent<Object>().quantity = quant;
        droppedItem.GetComponent<Object>().SetPhysicsEnabled(true);
        droppedItem.GetComponent<Rigidbody>().AddForce(handTransform.forward * 5f + transform.up * 2f, ForceMode.Impulse);
        OpenCloseDropUI();
    }

    public void AddItemToInventory(ItemData itemData, int quantity)
    {
        if (!itemData.isStackable)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i].itemData == null)
                {
                    inventorySlots[i].itemData = itemData;
                    inventorySlots[i].quantity = 1;
                    inventorySlotUIs[i].UpdateUI(itemData, 1);
                    return;
                }
            }
            return;
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].itemData == itemData)
            {
                int space = itemData.maxStackSize - inventorySlots[i].quantity;
                int addAmount = Mathf.Min(space, quantity);
                inventorySlots[i].quantity += addAmount;
                if(handItem == itemData)
                {
                    handItemGO.GetComponent<Object>().quantity += addAmount;
                }
                inventorySlotUIs[i].UpdateUI(itemData, inventorySlots[i].quantity);
                quantity -= addAmount;
                if (quantity <= 0) return;
            }
        }
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].itemData == null)
            {
                int addAmount = Mathf.Min(itemData.maxStackSize, quantity);
                inventorySlots[i].itemData = itemData;
                inventorySlots[i].quantity = addAmount;
                inventorySlotUIs[i].UpdateUI(itemData, addAmount);
                quantity -= addAmount;
                if (quantity <= 0) return;
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
                    if (selectedSlotIndex == index) return;
                    
                    selectedSlotIndex = index;
                    if (handItemGO != null)
                    {
                        Destroy(handTransform.GetChild(0).gameObject);
                        UpdateHandItem(index);
                    }
                    else
                    {
                        UpdateHandItem(index);
                    }
                }
            }
        }
    }

    private void UpdateHandItem(int index)
    {
        if (handTransform.childCount > 0) Destroy(handTransform.GetChild(0).gameObject);

        ItemData itemData = inventorySlots[index].itemData;
        handItem = itemData;

        GameObject newItem = Instantiate(itemData.itemPrefab, handTransform.position, Quaternion.identity, handTransform);
        newItem.transform.localRotation = Quaternion.Euler(newItem.GetComponent<Object>().item.handRotation);
        newItem.transform.localPosition = newItem.GetComponent<Object>().item.handPosition;
        newItem.GetComponent<Object>().quantity = inventorySlots[index].quantity;
        newItem.GetComponent<Object>().inHand = true;
        newItem.GetComponent<Object>().SetPhysicsEnabled(false);

        handItemGO = newItem;
    }
}
