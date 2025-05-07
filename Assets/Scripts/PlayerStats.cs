// Filename: Scripts/PlayerStats.cs
using UnityEngine;
using TMPro; // For TextMeshPro UI elements
using UnityEngine.Events; // For UnityEvents

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    public int CurrentHealth { get; private set; }
    public int CurrentBlock { get; private set; }
    
    public int CurrentMana { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI blockText;

    // Events for other systems to subscribe to (e.g., game over, sound effects)
    public UnityEvent OnPlayerDied;
    public UnityEvent OnHealthChanged;
    public UnityEvent OnBlockChanged;

    void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentBlock = 0;
        UpdateUI();
    }

    public void Initialize(int startingHealth, int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        CurrentHealth = startingHealth;
        CurrentBlock = 0; // Reset block at the start of combat/turn usually
        UpdateUI();
        OnHealthChanged.Invoke();
        OnBlockChanged.Invoke();
    }

    public void AddBlock(int amount)
    {
        if (amount <= 0) return;
        CurrentBlock += amount;
        Debug.Log($"Player gained {amount} block. Total block: {CurrentBlock}");
        UpdateUI();
        OnBlockChanged.Invoke();
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0) return;
        Debug.Log($"Player taking {damageAmount} damage.");

        int damageRemaining = damageAmount;

        // Apply block first
        if (CurrentBlock > 0)
        {
            if (CurrentBlock >= damageRemaining)
            {
                CurrentBlock -= damageRemaining;
                damageRemaining = 0;
                Debug.Log($"Damage fully absorbed by block. Block remaining: {CurrentBlock}");
            }
            else
            {
                damageRemaining -= CurrentBlock;
                CurrentBlock = 0;
                Debug.Log($"Block broken. Damage remaining: {damageRemaining}");
            }
            OnBlockChanged.Invoke();
        }

        // Apply remaining damage to health
        if (damageRemaining > 0)
        {
            CurrentHealth -= damageRemaining;
            Debug.Log($"Player health reduced by {damageRemaining}. Current health: {CurrentHealth}");
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
        if (CurrentHealth > maxHealth)
        {
            CurrentHealth = maxHealth;
        }
        Debug.Log($"Player healed for {amount}. Current health: {CurrentHealth}");
        UpdateUI();
        OnHealthChanged.Invoke();
    }

    // Called at the start of player's turn to clear block from previous turn
    public void ClearBlock()
    {
        CurrentBlock = 0;
        Debug.Log("Player block cleared at start of turn.");
        UpdateUI();
        OnBlockChanged.Invoke();
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        OnPlayerDied.Invoke();
        // Handle game over logic here or via the event
    }

    private void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {CurrentHealth} / {maxHealth}";
        }
        if (blockText != null)
        {
            blockText.text = $"Block: {CurrentBlock}";
        }
    }
}