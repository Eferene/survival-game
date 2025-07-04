using UnityEngine;

public class PlayerGeneral : MonoBehaviour
{
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthDecreaseRate;
    public float CurrentHealth
    {
        get => currentHealth;
        set { currentHealth = Mathf.Clamp(value, 0, maxHealth); }
    }

    [SerializeField] private float currentStamina;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDecreaseRate;
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
        if (CurrentStamina > 0) CurrentStamina -= Time.deltaTime * staminaDecreaseRate;
        if (CurrentHunger > 0) CurrentHunger -= Time.deltaTime * hungerDecreaseRate;
        if (CurrentThirst > 0) CurrentThirst -= Time.deltaTime * thirstDecreaseRate;
    }
}
