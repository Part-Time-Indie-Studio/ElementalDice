using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDeck playerDeck; // Assign your PlayerDeck GameObject
    [SerializeField] private DieThemeData dieTheme; // Assign your DieThemeData ScriptableObject
    [SerializeField] private List<Transform> handSlots; // Assign your 5 hand slot Transforms in order

    [Header("Hand Settings")]
    [SerializeField] private int startingHandSize = 5; // How many dice to draw initially

    // Track the data and the visual GameObject for each slot
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

        // Get the correct prefab for the die's shape
        GameObject diePrefab = dieTheme.GetPrefabForSides(drawnDieData.sides);
        if (diePrefab == null)
        {
            Debug.LogError($"PlayerHand: No prefab found in DieThemeData for sides: {drawnDieData.sides}. Cannot instantiate die visual.");
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

        // Add the data and visual instance to our tracking lists
        diceDataInHand.Add(drawnDieData);
        diceVisualsInHand.Add(dieVisualInstance);

        Debug.Log($"Drew die {drawnDieData.name} into hand slot {emptySlotIndex}.");
    }
}