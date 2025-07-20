using UnityEngine;
using UnityEngine.UI;

public class PlayerGeneral : MonoBehaviour
{
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

    private void Start()
    {
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
    }
}
