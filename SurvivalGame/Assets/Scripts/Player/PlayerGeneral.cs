using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;

public class PlayerGeneral : MonoBehaviour
{
    private Input playerInputActions;

    [Header("Player Values")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthDecreaseRate;
    public float CurrentHealth
    {
        get => currentHealth;
        set { currentHealth = Mathf.Clamp(value, 0, maxHealth); }
    }

    [SerializeField] private float currentStamina;
    public float maxStamina = 100f;
    public float staminaDecreaseRate;
    public float staminaIncreaseRate;
    public float jumpStaminaCost;
    public float CurrentStamina
    {
        get => currentStamina;
        set { currentStamina = Mathf.Clamp(value, 0, maxStamina); }
    }

    [SerializeField] private float currentHunger;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hungerDecreaseRate;
    public float CurrentHunger
    {
        get => currentHunger;
        set { currentHunger = Mathf.Clamp(value, 0, maxHunger); }
    }

    [SerializeField] private float currentThirst;
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float thirstDecreaseRate;
    public float CurrentThirst
    {
        get => currentThirst;
        set { currentThirst = Mathf.Clamp(value, 0, maxThirst); }
    }

    [Header("Player Values UI")]
    [SerializeField] private Image healthBar;
    //[SerializeField] private Image staminaBar;
    [SerializeField] private Image hungerBar;
    [SerializeField] private Image thirstBar;
    [SerializeField] private RectTransform staminaBar;
    [SerializeField] private GameObject staminaBG;

    [Header("Player Inventory & Item System")]
    public GameObject damageTextPrefab;
    private PlayerInventory playerInventory;
    private float lastHitTime = -1f;
    private float hitCooldown = 0.5f;

    [Header("Raycast & Input Settings")]
    public bool canHit;

    [Header("Other Things")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private TextMeshProUGUI characterDialogText;

    private void Awake()
    {
        playerInputActions = new Input();
    }

    void OnEnable()
    {
        playerInputActions.Player.Enable();
    }

    void OnDisable()
    {
        playerInputActions.Player.Disable();
    }

    private void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();

        CurrentHealth = maxHealth;
        CurrentStamina = maxStamina;
        CurrentHunger = maxHunger;
        CurrentThirst = maxThirst;
    }

    private void Update()
    {
        if (CurrentHealth > 0) CurrentHealth -= Time.deltaTime * healthDecreaseRate;
        if (CurrentHunger > 0) CurrentHunger -= Time.deltaTime * hungerDecreaseRate;
        if (CurrentThirst > 0) CurrentThirst -= Time.deltaTime * thirstDecreaseRate;

        healthBar.fillAmount = CurrentHealth / maxHealth;
        //staminaBar.fillAmount = CurrentStamina / maxStamina;
        hungerBar.fillAmount = CurrentHunger / maxHunger;
        thirstBar.fillAmount = CurrentThirst / maxThirst;
        staminaBar.localScale = new Vector3(CurrentStamina / maxStamina, 1, 1);
        if (currentStamina < maxStamina) staminaBG.gameObject.SetActive(true);
        else staminaBG.gameObject.SetActive(false);

        #region Animator & Animations
        if (playerInventory.handItemGO != null)
        {
            if (playerInventory.handItemGO.GetComponent<Animator>() != null)
            {
                if (playerInputActions.Player.Hit.triggered)
                {
                    if (Time.time - lastHitTime > hitCooldown)
                    {
                        playerInventory.handItemGO.GetComponent<Animator>().SetTrigger("Hit");
                    }
                }
            }
        }
        #endregion
        //Raycast
        RaycastHit hit;
        #region Item Alma Raycast
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, Mathf.Infinity))
        {
            if (hit.distance < 5)
            {
                if (hit.collider.CompareTag("Item") || hit.collider.CompareTag("Consumable"))
                {
                    if (canHit && playerInputActions.Player.Interaction.triggered)
                    {
                        if (hit.collider.gameObject.GetComponent<Object>().item != null)
                        {
                            Object obj = hit.collider.gameObject.GetComponent<Object>();
                            bool itemAdded = false;

                            foreach (var slot in playerInventory.inventorySlots)
                            {
                                if (slot.itemData == obj.item)
                                {
                                    int freeSpace = slot.itemData.maxStackSize - slot.quantity;
                                    if (freeSpace > 0)
                                    {
                                        int toAdd = Mathf.Min(freeSpace, obj.quantity);
                                        playerInventory.AddItemToInventory(obj.item, toAdd);
                                        obj.quantity -= toAdd;
                                        itemAdded = true;

                                        if (obj.quantity <= 0)
                                        {
                                            Destroy(obj.gameObject);
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!itemAdded && obj.quantity > 0)
                            {
                                foreach (var slot in playerInventory.inventorySlots)
                                {
                                    if (slot.itemData == null)
                                    {
                                        playerInventory.AddItemToInventory(obj.item, obj.quantity);
                                        Destroy(obj.gameObject);
                                        itemAdded = true;
                                        break;
                                    }
                                }
                            }

                            if (!itemAdded)
                            {
                                ShowDialogue("Inventory is full!");
                            }
                        }
                    }
                }
            }
            #endregion
            #region Hit Raycast
            if (playerInventory.handItem != null && playerInventory.handItem.itemType == ItemType.Tool)
            {
                if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, Mathf.Infinity))
                {
                    if (hit.distance < 3)
                    {
                        ToolItem toolItem = playerInventory.handItem as ToolItem;
                        for (int i = 0; i < toolItem.effectiveTags.Length; i++)
                        {
                            if (toolItem.effectiveTags.Contains(hit.collider.tag))
                            {
                                if (canHit && playerInputActions.Player.Hit.triggered && Time.time - lastHitTime > hitCooldown)
                                {
                                    if (hit.collider.gameObject.GetComponent<Breakable>() != null)
                                    {
                                        Breakable breakable = hit.collider.gameObject.GetComponent<Breakable>();
                                        int dmg = Random.Range(toolItem.minEfficiency, toolItem.maxEfficiency + 1);
                                        breakable.TakeDamage(dmg);
                                        lastHitTime = Time.time;
                                        GameObject damageText = Instantiate(damageTextPrefab, hit.point, Quaternion.identity);
                                        damageText.GetComponent<TextMeshPro>().text = dmg.ToString();
                                        damageText.transform.localScale = Vector3.one * 0.2f;
                                        damageText.transform.DOMoveY(damageText.transform.position.y + 1f, 1f).OnComplete(() => Destroy(damageText));
                                    }
                                }
                            }
                            else if (!toolItem.effectiveTags.Contains(hit.collider.tag) && hit.collider.tag != "Ground" && hit.collider.tag != "Untagged" && hit.collider.tag != "Item" && hit.collider.tag != "Consumable")
                            {
                                if (canHit && playerInputActions.Player.Hit.triggered && Time.time - lastHitTime > hitCooldown)
                                {
                                    lastHitTime = Time.time;
                                    GameObject damageText = Instantiate(damageTextPrefab, hit.point, Quaternion.identity);
                                    damageText.GetComponent<TextMeshPro>().text = "0";
                                    damageText.transform.localScale = Vector3.one * 0.2f;
                                    damageText.transform.DOMoveY(damageText.transform.position.y + 1f, 1f).OnComplete(() => Destroy(damageText));
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            else if (playerInventory.handItem != null && playerInventory.handItem.itemType == ItemType.Consumable)
            {
                if (canHit && playerInputActions.Player.Hit.triggered)
                {
                    ConsumableItem consumableItem = playerInventory.handItem as ConsumableItem;
                    if (consumableItem != null)
                    {
                        UseConsumable(consumableItem);
                        playerInventory.handItemGO.GetComponent<Object>().quantity -= 1;
                        playerInventory.inventorySlots[playerInventory.selectedSlotIndex].quantity -= 1;

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
                            playerInventory.inventorySlotUIs[playerInventory.selectedSlotIndex].UpdateUI(consumableItem, playerInventory.handItemGO.GetComponent<Object>().quantity);
                        }
                    }
                }
            }
        }
    }

    private void UseConsumable(ConsumableItem consumableItem)
    {
        if (consumableItem.healthRestore > 0)
        {
            CurrentHealth += consumableItem.healthRestore;
        }
        if (consumableItem.staminaRestore > 0)
        {
            CurrentStamina += consumableItem.staminaRestore;
        }
        if (consumableItem.hungerRestore > 0)
        {
            CurrentHunger += consumableItem.hungerRestore;
        }
        if (consumableItem.thirstRestore > 0)
        {
            CurrentThirst += consumableItem.thirstRestore;
        }
    }

    public void ShowDialogue(string message)
    {
        characterDialogText.transform.localScale = Vector3.one;
        characterDialogText.text = message;
        characterDialogText.gameObject.SetActive(true);
        Tween myTween = characterDialogText.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        DOVirtual.DelayedCall(2f, () =>
        {
            myTween.Kill();
            characterDialogText.gameObject.SetActive(false);
        });
    }
}
