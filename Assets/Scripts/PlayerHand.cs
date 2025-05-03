using UnityEngine;
using System.Collections.Generic;

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

    // Prepare the hand lists
    void InitializeHand()
    {
        diceDataInHand.Clear();
        diceVisualsInHand.Clear();
    }

    // Draws the initial hand at the start
    void DrawInitialHand()
    {
        Debug.Log("Drawing initial hand...");
        for (int i = 0; i < startingHandSize; i++)
        {
            // Break loop early if hand becomes full during initial draw
            if (diceVisualsInHand.Count >= handSlots.Count) break;
            DrawSingleDieToHand();
        }
        Debug.Log($"Initial hand draw complete. Dice in hand: {diceVisualsInHand.Count}");
    }

    // Draws one die from the deck into the next available hand slot
    public void DrawSingleDieToHand()
    {

        // Draw from the deck
        DieData drawnDieData = playerDeck.DrawDie();

        // Check if a die was successfully drawn
        if (drawnDieData == null)
        {
            Debug.LogWarning("PlayerHand: Attempted to draw but deck returned null (likely empty).");
            return;
        }

        // Find the next empty slot index
        int emptySlotIndex = diceVisualsInHand.Count;

        if (emptySlotIndex >= handSlots.Count)
        {
            Debug.LogError($"Calculated empty slot index {emptySlotIndex} is out of bounds for handSlots list (Size: {handSlots.Count}). Cannot place die.");
            // Maybe discard the drawn die?
            playerDeck.DiscardDie(drawnDieData);
            return;
        }

        // Get the correct prefab for the die's shape
        GameObject diePrefab = dieTheme.GetPrefabForSides(drawnDieData.sides);
        if (diePrefab == null)
        {
            Debug.LogError($"PlayerHand: No prefab found in DieThemeData for sides: {drawnDieData.sides}. Cannot instantiate die visual.");
            playerDeck.DiscardDie(drawnDieData);
            return;
        }

        // Instantiate the visual prefab as a child of the correct hand slot
        Transform slotTransform = handSlots[emptySlotIndex];
        GameObject dieVisualInstance = Instantiate(diePrefab, slotTransform);

        // Reset local transform 
        dieVisualInstance.transform.localPosition = Vector3.zero;
        dieVisualInstance.transform.localRotation = Quaternion.identity;
        dieVisualInstance.transform.localScale = Vector3.one;

        // Get the visual controller component from the instantiated die
        DieVisualController_SpriteRenderer visualController = dieVisualInstance.GetComponent<DieVisualController_SpriteRenderer>();
        if (visualController == null)
        {
            Debug.LogError($"PlayerHand: Instantiated die prefab '{diePrefab.name}' is missing the DieVisualController_SpriteRenderer script.", dieVisualInstance);
            Destroy(dieVisualInstance); // Clean up the broken instance
            return;
        }

        // Tell the visual controller to display the drawn die's data
        visualController.DisplayDie(drawnDieData);

        DraggableDie draggable = dieVisualInstance.GetComponent<DraggableDie>();
        if (draggable != null)
        {
            // Call Initialize to pass the PlayerHand reference, the DieData, and the starting parent transform
            draggable.Initialize(this, drawnDieData, slotTransform);
            Debug.Log($"PlayerHand: Initialized DraggableDie on {dieVisualInstance.name}.");
        }
        else
        {
            // Log an error if the DraggableDie script is missing from the prefab
            Debug.LogError($"PlayerHand: Instantiated die prefab '{diePrefab.name}' is missing the DraggableDie script!", dieVisualInstance);
            // Clean up the instance since it won't be draggable
            Destroy(dieVisualInstance);
            // No need to discard drawnDieData here
            return; // Stop processing this die
        }

        // Add the data and visual instance to our tracking lists
        diceDataInHand.Add(drawnDieData);
        diceVisualsInHand.Add(dieVisualInstance);

        Debug.Log($"Drew die {drawnDieData.name} into hand slot {emptySlotIndex}.");
    }

    public void NotifyDiePlacedOnGrid(GameObject dieVisualInstance)
    {
        int index = diceVisualsInHand.FindIndex(visual => visual == dieVisualInstance);

        if (index != -1) 
        {
            Debug.Log($"PlayerHand: Received notification that die visual '{dieVisualInstance.name}' was placed on grid. Found at index {index}.");

            DieData data = null;
            if (index < diceDataInHand.Count)
            {
                data = diceDataInHand[index];
                Debug.Log($"PlayerHand: Corresponding data is '{data?.name ?? "Unknown"}'. Removing from both lists.");
            }
            else
            {
                Debug.LogError($"PlayerHand: Mismatch between visual list and data list sizes! Index {index} is out of bounds for data list (Size: {diceDataInHand.Count}). Removing visual only.");
            }

            diceVisualsInHand.RemoveAt(index);

            if (index < diceDataInHand.Count) 
            {
                diceDataInHand.RemoveAt(index);
            }

            Debug.Log($"PlayerHand: Hand lists updated. Visuals count: {diceVisualsInHand.Count}, Data count: {diceDataInHand.Count}.");

        }
        else
        {
            // This might happen if the die was somehow not tracked correctly
            Debug.LogWarning($"PlayerHand: Could not find die visual {dieVisualInstance.name} in 'diceVisualsInHand' list to remove after grid placement notification.", dieVisualInstance);
        }
    }
}