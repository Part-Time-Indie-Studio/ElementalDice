// Filename: Scripts/PlayerStats.cs
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    public int CurrentHealth { get; private set; }
    public int CurrentBlock { get; private set; }
    [SerializeField] private int maxMana = 3; // Default max mana
    public int CurrentMana { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI blockText;
    [SerializeField] private TextMeshProUGUI manaText; // Add this in the Inspector

    [Header("Events")]
    public UnityEvent OnPlayerDied;
    public UnityEvent OnHealthChanged;
    public UnityEvent OnBlockChanged;
    public UnityEvent OnManaChanged; // New event for mana

    void Awake()
    {
        // Initialize with default values, CombatManager can override
        CurrentHealth = maxHealth;
        CurrentBlock = 0;
        CurrentMana = maxMana; // Start with full mana
        UpdateUI();
    }

    public void Initialize(int startingHealth, int newMaxHealth, int startingMana, int newMaxMana)
    {
        maxHealth = newMaxHealth;
        CurrentHealth = startingHealth;
        maxMana = newMaxMana;
        CurrentMana = startingMana;
        CurrentBlock = 0;
        UpdateUI();
        OnHealthChanged.Invoke();
        OnBlockChanged.Invoke();
        OnManaChanged.Invoke();
    }

    public void AddBlock(int amount)
    {
        if (amount <= 0) return;
        CurrentBlock += amount;
        UpdateUI();
        OnBlockChanged.Invoke();
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0) return;
        int damageRemaining = damageAmount;
        if (CurrentBlock > 0)
        {
            if (CurrentBlock >= damageRemaining)
            {
                CurrentBlock -= damageRemaining;
                damageRemaining = 0;
            }
            else
            {
                damageRemaining -= CurrentBlock;
                CurrentBlock = 0;
            }
            OnBlockChanged.Invoke();
        }
        if (damageRemaining > 0)
        {
            CurrentHealth -= damageRemaining;
            OnHealthChanged.Invoke();
        }
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            Die();
        }
        UpdateUI();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHealth += amount;
        if (CurrentHealth > maxHealth) CurrentHealth = maxHealth;
        UpdateUI();
        OnHealthChanged.Invoke();
    }

    public void ClearBlock()
    {
        CurrentBlock = 0;
        UpdateUI();
        OnBlockChanged.Invoke();
    }
    
    public bool SpendMana(int amount)
    {
        if (amount < 0) return false; // Cannot spend negative mana
        if (CurrentMana >= amount)
        {
            CurrentMana -= amount;
            Debug.Log($"Player spent {amount} mana. Remaining: {CurrentMana}");
            UpdateUI();
            OnManaChanged.Invoke();
            return true;
        }
        Debug.Log($"Player has insufficient mana. Tried to spend {amount}, has {CurrentMana}");
        return false;
    }

    public void GainMana(int amount)
    {
        if (amount <= 0) return;
        CurrentMana += amount;
        if (CurrentMana > maxMana)
        {
            CurrentMana = maxMana;
        }
        Debug.Log($"Player gained {amount} mana. Total mana: {CurrentMana}");
        UpdateUI();
        OnManaChanged.Invoke();
    }

    public void RefillManaToMax()
    {
        CurrentMana = maxMana;
        Debug.Log($"Player mana refilled to {CurrentMana}");
        UpdateUI();
        OnManaChanged.Invoke();
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        OnPlayerDied.Invoke();
    }

    private void UpdateUI()
    {
        if (healthText != null) healthText.text = $"HP: {CurrentHealth} / {maxHealth}";
        if (blockText != null) blockText.text = $"Block: {CurrentBlock}";
        if (manaText != null) manaText.text = $"Mana: {CurrentMana} / {maxMana}"; // Update mana display
    }
}