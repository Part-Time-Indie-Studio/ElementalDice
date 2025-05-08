// Filename: Scripts/PlayerHand.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for .Any()

public class PlayerHand : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDeck playerDeck; // Assign your PlayerDeck instance here
    [SerializeField] private DieThemeData dieTheme; // Assign your DieThemeData asset here
    [SerializeField] private List<Transform> handSlots; // Assign your hand slot Transforms here

    [Header("Hand Settings")]
    [SerializeField] private int startingHandSize = 5; // Used for initial draw if needed, CombatManager will control turn draw

    // Internal tracking of dice in hand
    private List<DieData> diceDataInHand = new List<DieData>();
    private List<GameObject> diceVisualsInHand = new List<GameObject>();

    void Start()
    {
        // Initial setup, but CombatManager will typically control game start flow
        InitializeHand();
        // DrawInitialHand(); // CombatManager.StartPlayerTurnCycle now handles initial draw via DrawNewHand
    }

    void InitializeHand()
    {
        diceDataInHand.Clear();
        diceVisualsInHand.Clear();
        // Clear any existing GameObjects in hand slots from previous sessions (if any)
        foreach (Transform slot in handSlots)
        {
            if (slot != null)
            {
                foreach (Transform child in slot)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // This might be called by CombatManager at the very start of the game for the first hand
    public void DrawInitialHand()
    {
        Debug.Log("PlayerHand: Drawing initial hand...");
        DrawNewHand(startingHandSize); // Use the new system for consistency
    }

    public void DrawSingleDieToHand()
    {
        if (playerDeck == null) {
            Debug.LogError("PlayerDeck not assigned in PlayerHand! Cannot draw die.");
            return;
        }

        DieData drawnDieData = playerDeck.DrawDie();
        if (drawnDieData == null)
        {
            Debug.LogWarning("PlayerHand: Attempted to draw but deck returned null (likely empty).");
            return;
        }

        Transform slotTransform = null;
        // Find the first visually empty hand slot
        for(int i = 0; i < handSlots.Count; i++)
        {
            // A slot is empty if it's not the parent of any die currently tracked in diceVisualsInHand
            // or more simply, if it has no children (assuming dice are direct children)
            if (handSlots[i] != null && handSlots[i].childCount == 0)
            {
                 bool isOccupiedByTrackedDie = diceVisualsInHand.Any(d => d != null && d.transform.parent == handSlots[i]);
                 if (!isOccupiedByTrackedDie) {
                    slotTransform = handSlots[i];
                    break;
                 }
            }
        }
        
        // Fallback if childCount check isn't perfect, ensure we don't exceed list capacity based on tracked visuals
        if (slotTransform == null && diceVisualsInHand.Count < handSlots.Count) {
            // Try to find a slot that isn't parent to any of our *tracked* visuals
             for(int i = 0; i < handSlots.Count; i++) {
                if(!diceVisualsInHand.Any(d => d.transform.parent == handSlots[i])) {
                    slotTransform = handSlots[i];
                    break;
                }
            }
        }


        if (slotTransform == null)
        {
            Debug.LogWarning("PlayerHand: No empty hand slots available to draw a new die. Returning die to deck's discard pile.");
            playerDeck.DiscardDie(drawnDieData); 
            return;
        }
        
        GameObject diePrefab = dieTheme.GetPrefabForSides(drawnDieData.sides);
        if (diePrefab == null)
        {
            Debug.LogError($"PlayerHand: No prefab found for sides: {drawnDieData.sides}. Cannot instantiate die visual.");
            playerDeck.DiscardDie(drawnDieData);
            return;
        }

        GameObject dieVisualInstance = Instantiate(diePrefab, slotTransform);
        dieVisualInstance.transform.localPosition = Vector3.zero;
        dieVisualInstance.transform.localRotation = Quaternion.identity;
        dieVisualInstance.transform.localScale = Vector3.one;

        DieVisualControllerSpriteRenderer visualController = dieVisualInstance.GetComponent<DieVisualControllerSpriteRenderer>();
        int rollResult = 0; 

        if (visualController != null)
        {
            visualController.DisplayDie(drawnDieData);
            int maxRollValue = drawnDieData.GetMaxRollValue();
            rollResult = Random.Range(1, maxRollValue + 1);
            visualController.ShowRollResult(rollResult);
        }
        else
        {
            Debug.LogError($"PlayerHand: Instantiated die prefab '{diePrefab.name}' is missing DieVisualController_SpriteRenderer.", dieVisualInstance);
            Destroy(dieVisualInstance); 
            playerDeck.DiscardDie(drawnDieData); 
            return;
        }

        DraggableDie draggable = dieVisualInstance.GetComponent<DraggableDie>();
        if (draggable != null)
        {
            draggable.Initialize(this, drawnDieData, slotTransform);
            draggable.SetCurrentRollValue(rollResult);
        }
        else
        {
            Debug.LogError($"PlayerHand: Instantiated die prefab '{diePrefab.name}' is missing DraggableDie script!", dieVisualInstance);
            Destroy(dieVisualInstance); 
            playerDeck.DiscardDie(drawnDieData); // Ensure data isn't lost
            return;
        }

        diceDataInHand.Add(drawnDieData);
        diceVisualsInHand.Add(dieVisualInstance);
        // Debug.Log($"Drew die {drawnDieData.name} into slot: {slotTransform.name}. Rolled: {rollResult}");
    }

    public void DiscardCurrentHand()
    {
        Debug.Log("PlayerHand: Discarding current hand.");
        if (playerDeck == null)
        {
            Debug.LogError("PlayerDeck reference missing in PlayerHand. Cannot discard dice data properly.");
        }

        // Iterate backwards because we might be modifying the collection implicitly if Destroy triggers something
        // Or just use the count before clearing.
        for (int i = diceVisualsInHand.Count - 1; i >= 0; i--)
        {
            // Discard DieData
            if (i < diceDataInHand.Count && diceDataInHand[i] != null && playerDeck != null)
            {
                // Debug.Log($"Discarding {diceDataInHand[i].name} from hand to deck's discard pile.");
                playerDeck.DiscardDie(diceDataInHand[i]);
            }
            // Destroy GameObject
            if (diceVisualsInHand[i] != null)
            {
                Destroy(diceVisualsInHand[i]);
            }
        }
        diceVisualsInHand.Clear();
        diceDataInHand.Clear();
    }

    public void DrawNewHand(int numberOfDiceToDraw)
    {
        DiscardCurrentHand(); 
        
        Debug.Log($"PlayerHand: Drawing new hand of {numberOfDiceToDraw} dice.");
        for (int i = 0; i < numberOfDiceToDraw; i++)
        {
            if (diceVisualsInHand.Count >= handSlots.Count) 
            {
                Debug.LogWarning("PlayerHand: Cannot draw more dice, visual hand slots are full.");
                break; 
            }
            DrawSingleDieToHand(); 
        }
        Debug.Log($"PlayerHand: New hand drawn. Dice in hand: {diceVisualsInHand.Count}");
    }

    public void NotifyDiePlacedOnGrid(GameObject dieVisualInstance)
    {
        int index = diceVisualsInHand.IndexOf(dieVisualInstance);
        if (index != -1)
        {
            // Debug.Log($"PlayerHand: Die visual '{dieVisualInstance.name}' was placed on grid. Removing from hand tracking.");
            diceVisualsInHand.RemoveAt(index); 
            if (index < diceDataInHand.Count) // Check bounds for data list
            {
                diceDataInHand.RemoveAt(index);
            } else {
                 Debug.LogWarning($"PlayerHand: Index out of bounds for diceDataInHand while trying to remove for {dieVisualInstance.name}. Visuals may be out of sync with data.");
            }
        }
        // else { Debug.LogWarning($"PlayerHand: Could not find {dieVisualInstance.name} in visuals to remove."); }
    }

    public bool ReclaimDieToHand(GameObject dieVisualInstance, DieData dieData)
    {
        if (dieVisualInstance == null || dieData == null)
        {
            Debug.LogError("PlayerHand: ReclaimDieToHand called with null die visual or data.");
            return false;
        }

        Transform targetSlot = null;
        int existingVisualIndex = diceVisualsInHand.IndexOf(dieVisualInstance);

        if (existingVisualIndex != -1) // Die is already known and tracked
        {
            // Debug.Log($"PlayerHand: Die {dieVisualInstance.name} is already in hand lists. Ensuring it's properly parented.");
            if (dieVisualInstance.transform.parent != null && handSlots.Contains(dieVisualInstance.transform.parent))
            {
                targetSlot = dieVisualInstance.transform.parent; // It's already in a valid hand slot
            }
        }
        
        if (targetSlot == null) // If not already in a valid slot, or it's new to hand (from grid)
        {
            // Find truly empty slot (no other tracked die is parented to it)
            for (int i = 0; i < handSlots.Count; i++)
            {
                bool slotIsCurrentlyOccupied = false;
                foreach (GameObject visualInHand in diceVisualsInHand)
                {
                    // If checking a die that's already in our list (existingVisualIndex != -1)
                    // make sure we don't consider its *current* slot occupied by *itself* if we are trying to move it.
                    if (visualInHand == dieVisualInstance && existingVisualIndex != -1) continue; 

                    if (visualInHand.transform.parent == handSlots[i])
                    {
                        slotIsCurrentlyOccupied = true;
                        break;
                    }
                }
                if (!slotIsCurrentlyOccupied)
                {
                    targetSlot = handSlots[i];
                    break;
                }
            }
        }

        if (targetSlot != null)
        {
            dieVisualInstance.transform.SetParent(targetSlot);
            dieVisualInstance.transform.localPosition = Vector3.zero;
            dieVisualInstance.transform.localRotation = Quaternion.identity;
            dieVisualInstance.transform.localScale = Vector3.one;

            if (existingVisualIndex == -1) // If it was not in the lists (e.g., came from grid)
            {
                diceVisualsInHand.Add(dieVisualInstance);
                diceDataInHand.Add(dieData); // Add corresponding data
            }
            // Debug.Log($"PlayerHand: Die {dieVisualInstance.name} now in hand slot {targetSlot.name}.");
            return true;
        }

        Debug.LogWarning($"PlayerHand: No suitable hand slot found for {dieVisualInstance.name}. Hand might be full.");
        return false;
    }

    public List<Transform> GetHandSlotTransforms()
    {
        return handSlots;
    }
}