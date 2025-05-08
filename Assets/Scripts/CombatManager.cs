// Filename: Scripts/CombatManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

// Define a custom UnityEvent that can pass DieData, its roll value, and the GridSlot it came from
[System.Serializable]
public class ProcessDieActionEvent : UnityEvent<DieData, int, GridSlot> { }

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Character References")]
    public PlayerStats playerStats; // Assign in Inspector
    private Enemy currentEnemyInstance;      // Assign in Inspector
    public PlayerHand playerHand;   // Assign in Inspector
    
    [Header("Enemy Management")]
    [SerializeField] private List<GameObject> enemyPrefabs; // Assign your enemy prefabs in order
    [SerializeField] private Transform enemySpawnPoint;   // Assign a Transform where enemies will spawn
    private int currentEnemyPrefabIndex = 0;

    [Header("UI Elements")]
    public Button playTurnButton;     // Assign in Inspector

    [Header("Combat Setup")]
    [SerializeField] private int playerStartingHealth = 100;
    [SerializeField] private int playerStartingMaxMana = 3;
    [SerializeField] private int handSize = 5; // Number of dice to draw each turn

    [Header("Turn Flow Events")]
    public UnityEvent OnSetupCombatComplete;
    public UnityEvent OnPlayerTurnStart;
    public UnityEvent OnPlayerActionPhaseStart;
    public ProcessDieActionEvent OnProcessSingleDie;
    public UnityEvent OnPlayerActionPhaseEnd;
    public UnityEvent OnEnemyTurnStart;
    public UnityEvent OnEnemyActionResolved;
    public UnityEvent OnEnemyTurnEnd;
    public UnityEvent OnAllEnemiesDefeated; // Changed from OnCombatEnd to be more specific
    public UnityEvent OnPlayerDefeated;            

    private bool isPlayerTurn = true;
    private List<GridSlot> slotsWithDiceToProcess = new List<GridSlot>();

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

        if (playerHand == null) playerHand = FindObjectOfType<PlayerHand>();
        if (playerHand == null) Debug.LogError("CombatManager CRITICAL: PlayerHand not found in scene or assigned!");
    }

    void Start()
    {
        if (playerStats == null || playerHand == null)
        {
            Debug.LogError("PlayerStats or PlayerHand not assigned/found in CombatManager! Combat cannot start properly.");
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError("No enemy prefabs assigned in CombatManager!");
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        if (enemySpawnPoint == null)
        {
            Debug.LogError("Enemy Spawn Point not assigned in CombatManager!");
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }

        SetupFirstCombat();

        if (playTurnButton != null)
        {
            playTurnButton.onClick.AddListener(HandlePlayTurnButtonPressed);
        }
        OnProcessSingleDie.RemoveListener(ResolveSingleDieAction);
        OnProcessSingleDie.AddListener(ResolveSingleDieAction);
    }
    
        void SetupFirstCombat()
    {
        Debug.Log("Setting up first combat...");
        currentEnemyPrefabIndex = 0; // Start with the first enemy in the list
        playerStats.Initialize(playerStartingHealth, playerStartingHealth, playerStartingMaxMana, playerStartingMaxMana);

        if (!SpawnEnemy(currentEnemyPrefabIndex))
        {
            Debug.LogError("Failed to spawn the first enemy. Check enemyPrefabs list.");
            if(playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        
        OnSetupCombatComplete?.Invoke();
        StartPlayerTurnCycle();
    }

    bool SpawnEnemy(int prefabIndex)
    {
        if (currentEnemyInstance != null)
        {
            Destroy(currentEnemyInstance.gameObject); // Destroy previous enemy if any
            currentEnemyInstance = null;
        }

        if (prefabIndex >= enemyPrefabs.Count || enemyPrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Enemy prefab at index {prefabIndex} is missing or index out of bounds.");
            return false; // No more enemies or invalid prefab
        }

        GameObject enemyGO = Instantiate(enemyPrefabs[prefabIndex], enemySpawnPoint.position, enemySpawnPoint.rotation, enemySpawnPoint);
        currentEnemyInstance = enemyGO.GetComponent<Enemy>();

        if (currentEnemyInstance == null)
        {
            Debug.LogError($"Spawned enemy prefab {enemyPrefabs[prefabIndex].name} is missing an Enemy component!");
            Destroy(enemyGO);
            return false;
        }
        
        // Enemy's Initialize method should set its health based on its own prefab's values or passed parameters
        // For example, Enemy.Initialize(enemySpecificMaxHealth, enemySpecificMaxHealth)
        // Let's assume Enemy.cs has an Initialize method that handles its own setup (e.g. from its serialized fields)
        currentEnemyInstance.InitializeFromStats(); // You'd add this method to Enemy.cs to use its own serialized maxHealth

        Debug.Log($"Spawned new enemy: {currentEnemyInstance.name}");
        return true;
    }

    void StartPlayerTurnCycle()
    {
        Debug.Log("--- Player's Turn Begun ---");
        isPlayerTurn = true;
        if (playerStats != null)
        {
            playerStats.ClearBlock();
            playerStats.RefillManaToMax();
        }
        if (currentEnemyInstance != null) currentEnemyInstance.PrepareIntent();

        if (playerHand != null) playerHand.DrawNewHand(handSize);
        else Debug.LogError("PlayerHand reference not set. Cannot draw new hand.");

        OnPlayerTurnStart?.Invoke();
        if (playTurnButton != null) playTurnButton.interactable = true;
    }

    void HandlePlayTurnButtonPressed()
    {
        if (!isPlayerTurn)
        {
            Debug.LogWarning("Not player's turn, but PlayTurnButton was pressed.");
            return;
        }
        
        if (playTurnButton != null) playTurnButton.interactable = false; 
        
        Debug.Log("Play Turn button pressed. Invoking OnPlayerActionPhaseStart.");
        OnPlayerActionPhaseStart?.Invoke(); 

        ProcessAndResolveDiceActions();
    }

    void ProcessAndResolveDiceActions()
    {
        Debug.Log("Processing Player Dice Actions...");
        slotsWithDiceToProcess.Clear(); 

        if (GridManager.Instance != null && GridManager.Instance.gridSlots != null)
        {
            foreach (GridSlot slot in GridManager.Instance.gridSlots)
            {
                if (slot != null && slot.IsOccupied && slot.OccupyingDie != null)
                {
                    slotsWithDiceToProcess.Add(slot);
                }
            }
        }
        else
        {
            Debug.LogError("GridManager or its slots are not available for processing dice!");
            OnPlayerActionPhaseEnd?.Invoke(); 
            StartEnemyTurnCycle(); 
            return;
        }

        if (slotsWithDiceToProcess.Count == 0)
        {
            Debug.Log("No dice on the grid to process.");
        }

        foreach (GridSlot slot in slotsWithDiceToProcess)
        {
            DraggableDie draggableDie = slot.OccupyingDie.GetComponent<DraggableDie>();
            if (draggableDie != null)
            {
                DieData dieData = draggableDie.GetDieData();
                int rollValue = draggableDie.CurrentRollValue;
                if (dieData != null) 
                {
                    // Debug.Log($"Invoking OnProcessSingleDie for {dieData.name} (Value: {rollValue}) in slot {slot.name}");
                    OnProcessSingleDie?.Invoke(dieData, rollValue, slot);
                }
                else Debug.LogError($"Die in slot {slot.name} is missing DieData!", slot.OccupyingDie);
            }
            else Debug.LogError($"OccupyingDie in slot {slot.name} is missing DraggableDie component!", slot.OccupyingDie);
        }
        
        CleanupProcessedDiceFromGrid();
        // Debug.Log("All dice processing events invoked and dice cleaned up. Invoking OnPlayerActionPhaseEnd.");
        OnPlayerActionPhaseEnd?.Invoke(); 
        StartEnemyTurnCycle();
    }
    
    public void ResolveSingleDieAction(DieData dieData, int rollValue, GridSlot sourceSlot)
    {
        if (dieData == null) { Debug.LogError("ResolveSingleDieAction: DieData is null.", sourceSlot); return; }
        if (sourceSlot == null) { Debug.LogError($"ResolveSingleDieAction: sourceSlot is null for die {dieData.name}."); return; }

        Debug.Log($"CombatManager Resolving: {dieData.name} (Action: {dieData.actionType}, Value: {rollValue}, Target: {dieData.targetType}) from slot {sourceSlot.name}");

        switch (dieData.actionType)
        {
            case DieActionType.Attack:
                if (dieData.targetType == TargetType.SingleEnemy && currentEnemyInstance  != null) currentEnemyInstance .TakeDamage(rollValue);
                else Debug.LogWarning($"Attack die {dieData.name} has unhandled target type: {dieData.targetType} or no enemy.");
                break;
            case DieActionType.Block:
                if (dieData.targetType == TargetType.Self && playerStats != null) playerStats.AddBlock(rollValue);
                else Debug.LogWarning($"Block die {dieData.name} has unhandled target type: {dieData.targetType} or no player stats.");
                break;
            case DieActionType.Heal:
                if (dieData.targetType == TargetType.Self && playerStats != null) playerStats.Heal(rollValue);
                else Debug.LogWarning($"Heal die {dieData.name} has unhandled target type: {dieData.targetType} or no player stats.");
                break;
            default:
                Debug.LogWarning($"Unhandled DieActionType: {dieData.actionType} for die {dieData.name}");
                break;
        }
    }

    void CleanupProcessedDiceFromGrid()
    {
        // Debug.Log("Cleaning up dice that were processed...");
        PlayerDeck playerDeck = FindObjectOfType<PlayerDeck>(); 
        if (playerDeck == null) Debug.LogError("PlayerDeck not found during CleanupProcessedDiceFromGrid. DieData won't be discarded to deck.");

        foreach (GridSlot slot in slotsWithDiceToProcess)
        {
            if (slot != null && slot.IsOccupied && slot.OccupyingDie != null)
            {
                DraggableDie draggableDie = slot.OccupyingDie.GetComponent<DraggableDie>();
                if (draggableDie != null)
                {
                    DieData dieData = draggableDie.GetDieData();
                    if (dieData != null && playerDeck != null) 
                    {
                        playerDeck.DiscardDie(dieData); 
                    }
                }
                Destroy(slot.OccupyingDie); 
                slot.RemoveDie();           
            }
        }
        slotsWithDiceToProcess.Clear();
    }

    void StartEnemyTurnCycle()
    {
        isPlayerTurn = false;
        Debug.Log("--- Player's Turn Ended. Enemy's Turn Begun ---");
        OnEnemyTurnStart?.Invoke();

        if (currentEnemyInstance != null && currentEnemyInstance.CurrentHealth > 0)
        {
            currentEnemyInstance.ClearBlock();
            currentEnemyInstance.ExecuteAction(playerStats); // Enemy performs its action
        }
        else if (currentEnemyInstance == null)
        {
            Debug.LogWarning("No current enemy instance to act.");
        }
        OnEnemyActionResolved?.Invoke();

        Debug.Log("--- Enemy's Turn Ended ---");
        OnEnemyTurnEnd?.Invoke();

        // Check for game over conditions
        if (playerStats != null && playerStats.CurrentHealth <= 0)
        {
            Debug.Log("Game Over - Player Defeated!");
            OnPlayerDefeated?.Invoke(); // Use a specific event for player defeat
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }

        if (currentEnemyInstance != null && currentEnemyInstance.CurrentHealth <= 0)
        {
            HandleEnemyDefeated(); // New method to handle enemy defeat and progression
            return; // HandleEnemyDefeated will decide if a new turn starts or game ends
        }
        
        // If no one died and enemy is still there (e.g. enemy didn't die this turn)
        if(currentEnemyInstance != null && currentEnemyInstance.CurrentHealth > 0) {
            StartPlayerTurnCycle(); // Loop back to player's turn
        }
    }
    
    void HandleEnemyDefeated()
    {
        Debug.Log($"Victory - Enemy {currentEnemyInstance.name} Defeated!");
        // Optional: Invoke an event specific to defeating one enemy
        // OnSingleEnemyDefeated?.Invoke(); 

        currentEnemyPrefabIndex++; // Move to the next enemy in the list

        if (currentEnemyPrefabIndex < enemyPrefabs.Count)
        {
            Debug.Log("Spawning next enemy...");
            if (!SpawnEnemy(currentEnemyPrefabIndex))
            {
                Debug.LogError("Failed to spawn next enemy. Ending combat as player technically won this wave.");
                OnAllEnemiesDefeated?.Invoke(); // Or some other event indicating no more enemies to spawn
                if (playTurnButton != null) playTurnButton.interactable = false;
                return;
            }
            // Player's stats (health, maxMana) carry over.
            // Block will be cleared and mana refilled by StartPlayerTurnCycle.
            StartPlayerTurnCycle(); // Start a new player turn against the new enemy
        }
        else
        {
            Debug.Log("All enemies in the sequence defeated! Player wins the game/run!");
            OnAllEnemiesDefeated?.Invoke(); // Signal overall victory
            if (playTurnButton != null) playTurnButton.interactable = false;
            // Handle game won logic (e.g., show victory screen)
        }
    }
}