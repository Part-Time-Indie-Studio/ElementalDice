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
    public Enemy currentEnemy;       // Assign in Inspector
    public PlayerHand playerHand;   // Assign in Inspector

    [Header("UI Elements")]
    public Button playTurnButton;     // Assign in Inspector

    [Header("Combat Setup")]
    [SerializeField] private int playerStartingHealth = 100;
    [SerializeField] private int playerStartingMaxMana = 3;
    [SerializeField] private int enemyStartingHealth = 75;
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
    public UnityEvent OnCombatEnd;              

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
        if (playerStats == null || currentEnemy == null || playerHand == null)
        {
            Debug.LogError("PlayerStats, Enemy, or PlayerHand not assigned/found in CombatManager! Combat cannot start properly.");
            if(playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        
        SetupNewCombat();

        if (playTurnButton != null)
        {
            playTurnButton.onClick.AddListener(HandlePlayTurnButtonPressed);
        }
        else
        {
            Debug.LogWarning("PlayTurnButton not assigned in CombatManager.");
        }
        
        OnProcessSingleDie.RemoveListener(ResolveSingleDieAction); 
        OnProcessSingleDie.AddListener(ResolveSingleDieAction);
    }

    void SetupNewCombat()
    {
        Debug.Log("Setting up new combat...");
        playerStats.Initialize(playerStartingHealth, playerStartingHealth, playerStartingMaxMana, playerStartingMaxMana);
        
        currentEnemy.gameObject.SetActive(true); 
        currentEnemy.Initialize(enemyStartingHealth, enemyStartingHealth);
        
        OnSetupCombatComplete?.Invoke();
        StartPlayerTurnCycle(); // This will now include drawing the initial hand
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
        if (currentEnemy != null) currentEnemy.PrepareIntent();   

        if (playerHand != null)
        {
            playerHand.DrawNewHand(handSize); // Discard old hand and draw a fresh one
        }
        else
        {
            Debug.LogError("PlayerHand reference not set in CombatManager. Cannot draw new hand.");
        }

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
                if (dieData.targetType == TargetType.SingleEnemy && currentEnemy != null) currentEnemy.TakeDamage(rollValue);
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

        if (currentEnemy != null && currentEnemy.CurrentHealth > 0) 
        {
            currentEnemy.ClearBlock();          
            currentEnemy.ExecuteAction(playerStats); 
        }
        OnEnemyActionResolved?.Invoke();

        Debug.Log("--- Enemy's Turn Ended ---");
        OnEnemyTurnEnd?.Invoke();

        if (playerStats != null && playerStats.CurrentHealth <= 0)
        {
            Debug.Log("Game Over - Player Defeated!");
            OnCombatEnd?.Invoke();
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        if (currentEnemy != null && currentEnemy.CurrentHealth <= 0)
        {
            Debug.Log("Victory - Enemy Defeated!");
            OnCombatEnd?.Invoke();
            if (playTurnButton != null) playTurnButton.interactable = false;
            return;
        }
        StartPlayerTurnCycle();
    }
}