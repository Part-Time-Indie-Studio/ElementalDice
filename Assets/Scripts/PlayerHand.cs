// Filename: Scripts/PlayerHand.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerHand : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDeck playerDeck;
    [SerializeField] private DieThemeData dieTheme;
    [SerializeField] private List<Transform> handSlots;

    [Header("Hand Settings")]
    [SerializeField] private int startingHandSize = 5;

    private List<DieData> diceDataInHand = new List<DieData>();
    private List<GameObject> diceVisualsInHand = new List<GameObject>();

    void Start()
    {
        InitializeHand();
        DrawInitialHand();
    }

    void InitializeHand()
    {
        diceDataInHand.Clear();
        diceVisualsInHand.Clear();
    }

    public void DrawInitialHand()
    {
        Debug.Log("Drawing initial hand...");
        for (int i = 0; i < startingHandSize; i++)
        {
            if (diceVisualsInHand.Count >= handSlots.Count) break;
            DrawSingleDieToHand();
        }
        Debug.Log($"Initial hand draw complete. Dice in hand: {diceVisualsInHand.Count}");
    }

    public void DrawSingleDieToHand()
    {
        DieData drawnDieData = playerDeck.DrawDie();
        if (drawnDieData == null)
        {
            Debug.LogWarning("PlayerHand: Attempted to draw but deck returned null (likely empty).");
            return;
        }

        // Find an empty slot
        Transform slotTransform = null;
        for(int i = 0; i < handSlots.Count; i++)
        {
            bool slotOccupied = diceVisualsInHand.Any(d => d.transform.parent == handSlots[i]);
            if (!slotOccupied)
            {
                slotTransform = handSlots[i];
                break;
            }
        }

        if (slotTransform == null)
        {
            Debug.LogWarning("PlayerHand: No empty hand slots available to draw a new die.");
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
        int rollResult = 0;
        DieVisualControllerSpriteRenderer visualController = dieVisualInstance.GetComponent<DieVisualControllerSpriteRenderer>();
        
        if (visualController != null)
        {
            visualController.DisplayDie(drawnDieData); // This sets up the die and calls HideRollResult()
            
            int maxRollValue = drawnDieData.GetMaxRollValue();
            rollResult = Random.Range(1, maxRollValue + 1); // Generates a number from 1 to maxRollValue
            
            Debug.Log($"Die {drawnDieData.name} (Sides: {drawnDieData.sides}) drawn to hand, initially rolled: {rollResult}");
            visualController.ShowRollResult(rollResult);
        }
        else
        {
            Debug.LogError($"PlayerHand: Instantiated die prefab '{diePrefab.name}' is missing DieVisualController_SpriteRenderer.", dieVisualInstance);
            Destroy(dieVisualInstance); // Clean up
            playerDeck.DiscardDie(drawnDieData); // Return die to deck
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
            Destroy(dieVisualInstance); // Clean up
            // No need to discard drawnDieData again if already handled by visual controller missing
            return;
        }

        diceDataInHand.Add(drawnDieData);
        diceVisualsInHand.Add(dieVisualInstance);
        Debug.Log($"Drew die {drawnDieData.name} into slot: {slotTransform.name}.");
    }

    public void NotifyDiePlacedOnGrid(GameObject dieVisualInstance)
    {
        int index = diceVisualsInHand.IndexOf(dieVisualInstance);
        if (index != -1)
        {
            Debug.Log($"PlayerHand: Die visual '{dieVisualInstance.name}' was placed on grid. Removing from hand tracking.");
            diceVisualsInHand.RemoveAt(index);
            if (index < diceDataInHand.Count)
            {
                diceDataInHand.RemoveAt(index);
            }
            else
            {
                Debug.LogError($"PlayerHand: Index mismatch when trying to remove data for {dieVisualInstance.name}. Data count: {diceDataInHand.Count}, Index: {index}");
            }
            Debug.Log($"PlayerHand: Hand lists updated. Visuals count: {diceVisualsInHand.Count}, Data count: {diceDataInHand.Count}.");
        }
        else
        {
            Debug.LogWarning($"PlayerHand: Could not find die visual {dieVisualInstance.name} in 'diceVisualsInHand' to remove after grid placement.", dieVisualInstance);
        }
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

        if (existingVisualIndex != -1)
        {
            Debug.Log($"PlayerHand: Die {dieVisualInstance.name} is already in hand lists. Ensuring it's properly parented.");
            if (dieVisualInstance.transform.parent != null && handSlots.Contains(dieVisualInstance.transform.parent))
            {
                targetSlot = dieVisualInstance.transform.parent;
            }
        }

        if (targetSlot == null)
        {
            for (int i = 0; i < handSlots.Count; i++)
            {
                bool slotIsCurrentlyOccupied = false;
                foreach (GameObject visualInHand in diceVisualsInHand)
                {
                    if (visualInHand == dieVisualInstance && visualInHand.transform.parent == handSlots[i]) continue;
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

            if (existingVisualIndex == -1) 
            {
                diceVisualsInHand.Add(dieVisualInstance);
                diceDataInHand.Add(dieData);
            }



            Debug.Log($"PlayerHand: Die {dieVisualInstance.name} now in hand slot {targetSlot.name}. Visuals: {diceVisualsInHand.Count}, Data: {diceDataInHand.Count}");
            return true;
        }

        Debug.LogWarning($"PlayerHand: No suitable hand slot found for {dieVisualInstance.name}. Hand might be full.");
        return false;
    }
}