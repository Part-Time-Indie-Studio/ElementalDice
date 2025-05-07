// Filename: Scripts/CombatManager.cs
using UnityEngine;
using UnityEngine.UI; // For Button

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Character References")]
    public PlayerStats playerStats;
    public Enemy currentEnemy; // Assuming one enemy for now

    [Header("UI Elements")]
    public Button playTurnButton; // Assign your "Play/Next Turn" button here

    [Header("Combat Setup")]
    [SerializeField] private int playerStartingHealth = 100;
    [SerializeField] private int enemyStartingHealth = 75;


    private bool isPlayerTurn = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (playerStats == null || currentEnemy == null)
        {
            Debug.LogError("PlayerStats or Enemy not assigned in CombatManager!");
            return;
        }

        SetupNewCombat();

        if (playTurnButton != null)
        {
            playTurnButton.onClick.AddListener(OnPlayTurnButtonPressed);
        }
        else
        {
            Debug.LogWarning("PlayTurnButton not assigned in CombatManager.");
        }
    }

    void SetupNewCombat()
    {
        Debug.Log("Setting up new combat...");
        playerStats.Initialize(playerStartingHealth, playerStartingHealth);
        currentEnemy.gameObject.SetActive(true); // Ensure enemy is active
        currentEnemy.Initialize(enemyStartingHealth, enemyStartingHealth);
        
        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        Debug.Log("--- Player's Turn Started ---");
        isPlayerTurn = true;
        playerStats.ClearBlock(); // Clear player's block from previous turn
        currentEnemy.PrepareIntent(); // Enemy decides what it will do next turn
        
        if (playTurnButton != null) playTurnButton.interactable = true;

        // TODO: Player draws dice, mana refills, etc.
        // For now, PlayerHand.DrawInitialHand happens once at game start.
        // Later, you'll add logic here to draw dice from PlayerHand/PlayerDeck.
        //FindObjectOfType<PlayerHand>()?.DrawInitialHand(); // Example for re-drawing, adjust as needed for your game flow
    }

    void OnPlayTurnButtonPressed()
    {
        if (!isPlayerTurn)
        {
            Debug.LogWarning("Not player's turn, but PlayTurnButton was pressed.");
            return;
        }
        
        if (playTurnButton != null) playTurnButton.interactable = false;
        Debug.Log("Play Turn button pressed. Processing dice actions...");

        ProcessPlayerDiceActions(); // We will implement this next
        // After processing, proceed to enemy turn
    }

    void ProcessPlayerDiceActions()
    {
        // --- THIS IS THE NEXT BIG STEP ---
        // 1. Iterate through GridManager.Instance.gridSlots
        // 2. For each slot with an OccupyingDie:
        //    a. Get the DieData and the CurrentRollValue from DraggableDie.
        //    b. Based on DieData.actionType and DieData.targetType:
        //       - If Attack: currentEnemy.TakeDamage(CurrentRollValue);
        //       - If Block: playerStats.AddBlock(CurrentRollValue);
        //       - If Heal: playerStats.Heal(CurrentRollValue); (Make sure heal doesn't exceed max HP)
        //    c. After processing, dice are usually consumed/discarded.
        //       - Remove die visual from slot, call GridSlot.RemoveDie().
        //       - Notify PlayerDeck to discard the DieData.
        //       - Destroy the die GameObject.
        Debug.Log("Player dice actions would be processed here.");


        // After actions, proceed to enemy turn
        EndPlayerTurnAndStartEnemyTurn();
    }

    void EndPlayerTurnAndStartEnemyTurn()
    {
        isPlayerTurn = false;
        Debug.Log("--- Player's Turn Ended ---");

        // Placeholder for enemy turn
        Debug.Log("--- Enemy's Turn Started ---");
        currentEnemy.ClearBlock(); // Enemy clears its block
        currentEnemy.ExecuteAction(playerStats); // Enemy performs its prepared action
        Debug.Log("--- Enemy's Turn Ended ---");

        // Check for game over conditions
        if (playerStats.CurrentHealth <= 0)
        {
            Debug.Log("Game Over - Player Defeated!");
            // Handle game over state
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        if (currentEnemy.CurrentHealth <= 0)
        {
            Debug.Log("Victory - Enemy Defeated!");
            // Handle victory state (e.g., next enemy, rewards)
            if (playTurnButton != null) playTurnButton.interactable = false; // Or set up for next combat
            return;
        }

        // Start next player turn
        StartPlayerTurn();
    }
}