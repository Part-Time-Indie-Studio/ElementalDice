// Filename: Scripts/Enemy.cs
using UnityEngine;
using TMPro; // For TextMeshPro UI elements
using UnityEngine.Events;

public enum EnemyActionType
{
    Attack,
    Block,
    Buff,
    Debuff
    // Add more as needed
}

public class Enemy : MonoBehaviour
{
    [Header("Base Stats (Editable in Prefab)")]
    [SerializeField] private int maxHealth = 50; // Default max health for this enemy type
    [SerializeField] private int minAttack = 5; 
    [SerializeField] private int maxAttack = 10;

    public int CurrentHealth { get; private set; }
    public int CurrentBlock { get; private set; }
    public int AttackIntentValue { get; private set; }
    public EnemyActionType NextActionType { get; private set; }

    [Header("UI References (Optional - can be auto-found or set on prefab)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI blockText;
    [SerializeField] private TextMeshProUGUI intentText;

    public UnityEvent OnEnemyDied;
    public UnityEvent OnHealthChanged;
    public UnityEvent OnBlockChanged;
    public UnityEvent OnIntentChanged;


    void Awake()
    {
        CurrentHealth = maxHealth;
        CurrentBlock = 0;
        UpdateUI();
    }
    
    public void InitializeFromStats()
    {
        CurrentHealth = maxHealth; // Start with full health defined in prefab
        CurrentBlock = 0;
        Debug.Log($"{gameObject.name} initialized with {CurrentHealth}/{maxHealth} HP.");
        FindAndAssignUITexts();
        UpdateUI();
        OnHealthChanged.Invoke();
        OnBlockChanged.Invoke();
        // PrepareIntent(); // CombatManager will call this at start of player's turn
    }
    
    // Overload or alternative if CombatManager dictates stats
    public void Initialize(int startingHealth, int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        CurrentHealth = startingHealth;
        CurrentBlock = 0;
        Debug.Log($"{gameObject.name} initialized with {CurrentHealth}/{maxHealth} HP (custom).");
        FindAndAssignUITexts();
        UpdateUI();
        OnHealthChanged.Invoke();
        OnBlockChanged.Invoke();
    }
    
    private void FindAndAssignUITexts()
    {
        if (healthText == null) healthText = GameObject.Find("EnemyHP_Text")?.GetComponent<TextMeshProUGUI>();
        if (blockText == null) blockText = GameObject.Find("EnemyBlock_Text")?.GetComponent<TextMeshProUGUI>();
        if (intentText == null) intentText = GameObject.Find("EnemyIntent_Text")?.GetComponent<TextMeshProUGUI>();
    }

    public void AddBlock(int amount)
    {
        if (amount <= 0) return;
        CurrentBlock += amount;
        Debug.Log($"Enemy gained {amount} block. Total block: {CurrentBlock}");
        UpdateUI();
        OnBlockChanged.Invoke();
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0) return;
        Debug.Log($"Enemy taking {damageAmount} damage.");
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
    
    // Called at the start of enemy's turn to clear its block
    public void ClearBlock()
    {
        CurrentBlock = 0;
        Debug.Log("Enemy block cleared at start of its turn.");
        UpdateUI();
        OnBlockChanged.Invoke();
    }

    public void PrepareIntent() // Call this at the start of player's turn
    {
        // Simple AI: always attacks for now
        NextActionType = EnemyActionType.Attack;
        AttackIntentValue = Random.Range(minAttack, maxAttack + 1);
        Debug.Log($"Enemy prepares intent: {NextActionType} for {AttackIntentValue}");
        UpdateUI();
        OnIntentChanged.Invoke();
    }

    // This would be called when it's the enemy's turn to act
    public void ExecuteAction(PlayerStats player)
    {
        if (player == null) return;

        Debug.Log($"Enemy executing action: {NextActionType} for {AttackIntentValue}");
        switch (NextActionType)
        {
            case EnemyActionType.Attack:
                player.TakeDamage(AttackIntentValue);
                break;
            case EnemyActionType.Block:
                AddBlock(AttackIntentValue); // Or a different value for block
                break;
            // Add other actions later
        }
        // After executing, the enemy might choose a new intent for the *next* turn,
        // or this is done when the player's turn starts.
        // For now, PrepareIntent will be called by CombatManager.
    }

    private void Die()
    {
        Debug.Log("Enemy has died!");
        OnEnemyDied.Invoke();
        gameObject.SetActive(false); // Simple way to remove enemy
    }

    private void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Enemy HP: {CurrentHealth} / {maxHealth}";
        }
        if (blockText != null)
        {
            blockText.text = $"Enemy Block: {CurrentBlock}";
        }
        if (intentText != null)
        {
            switch (NextActionType)
            {
                case EnemyActionType.Attack:
                    intentText.text = $"Attacks for {AttackIntentValue}";
                    break;
                case EnemyActionType.Block:
                    intentText.text = $"Blocks for {AttackIntentValue}"; // Assuming intent value used for block
                    break;
                default:
                    intentText.text = "Thinking...";
                    break;
            }
        }
    }
}