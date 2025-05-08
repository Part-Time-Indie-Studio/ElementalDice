// Filename: Scripts/DraggableDie.cs
using UnityEngine;
using System.Collections.Generic; // Required for List<Transform>

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableDie : MonoBehaviour
{
    [Header("Dragging Settings")]
    [SerializeField] private float snapDistance = 1.5f;
    

    [Header("Hand Slot Drop Settings")] // New field for this logic
    [SerializeField] private float handSlotDropDetectionRadius = 0.75f; // How close to a slot center to count as a drop

    private Camera mainCamera;
    private Rigidbody2D rb;
    private Vector3 offset;
    private Vector3 startWorldPosition;
    private bool isDragging = false;

    private PlayerHand playerHand; // Already exists, used to get slot transforms
    private DieData dieData;
    public int CurrentRollValue { get; private set; }

    private Transform parentBeforeDrag;
    private GridSlot slotOccupiedBeforeDrag;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (mainCamera == null) { Debug.LogError("DraggableDie: No camera tagged 'MainCamera' found!"); }
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Initialize(PlayerHand hand, DieData data, Transform startingParent)
    {
        this.playerHand = hand; // Critical: playerHand is assigned here
        this.dieData = data;
        transform.SetParent(startingParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
    public void SetCurrentRollValue(int rollValue) { this.CurrentRollValue = rollValue; }
    public DieData GetDieData() { return dieData; }

    void OnMouseDown()
    {
        if (playerHand == null || mainCamera == null || dieData == null) return;
        isDragging = true;
        startWorldPosition = transform.position;
        offset = startWorldPosition - GetMouseWorldPos();
        parentBeforeDrag = transform.parent;

        if (parentBeforeDrag != null)
        {
            slotOccupiedBeforeDrag = parentBeforeDrag.GetComponent<GridSlot>();
            if (slotOccupiedBeforeDrag != null && slotOccupiedBeforeDrag.OccupyingDie == gameObject)
            {
                slotOccupiedBeforeDrag.RemoveDie(); 
            }
            else
            {
                slotOccupiedBeforeDrag = null; 
            }
        }
        else { slotOccupiedBeforeDrag = null; }
        transform.SetParent(null); 
    }
     void OnMouseDrag()
    {
        if (!isDragging) return;
        rb.MovePosition(GetMouseWorldPos() + offset);
    }

    // OnMouseUp needs to use the updated CheckAndPlaceInHandZone
    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        GridSlot closestGridSlot = FindClosestGridSlot();
        bool placedSuccessfullyOnGrid = false;

        if (closestGridSlot != null)
        {
            if (closestGridSlot == slotOccupiedBeforeDrag) 
            {
                if (closestGridSlot.PlaceDie(gameObject, this.dieData))
                {
                    SetDieVisualsOnParent(closestGridSlot.transform);
                    placedSuccessfullyOnGrid = true; 
                }
            }
            else if (!closestGridSlot.IsOccupied) 
            {
                if (slotOccupiedBeforeDrag == null) 
                {
                    if (CombatManager.Instance != null && CombatManager.Instance.playerStats != null)
                    {
                        PlayerStats currentPlayerStats = CombatManager.Instance.playerStats;
                        if (currentPlayerStats.CurrentMana >= this.dieData.manaCost)
                        {
                            if (closestGridSlot.PlaceDie(gameObject, this.dieData))
                            {
                                currentPlayerStats.SpendMana(this.dieData.manaCost);
                                SetDieVisualsOnParent(closestGridSlot.transform);
                                if (playerHand != null) playerHand.NotifyDiePlacedOnGrid(gameObject);
                                placedSuccessfullyOnGrid = true;
                            }
                        } else { Debug.Log($"Insufficient mana for {this.dieData.name}. Cost: {this.dieData.manaCost}, Have: {currentPlayerStats.CurrentMana}"); }
                    } else { Debug.LogError("CombatManager/PlayerStats not found for mana check during placement.");}
                }
                else 
                {
                    if (closestGridSlot.PlaceDie(gameObject, this.dieData))
                    {
                        SetDieVisualsOnParent(closestGridSlot.transform);
                        placedSuccessfullyOnGrid = true;
                    }
                }
            }
        }

        if (placedSuccessfullyOnGrid)
        {
             Debug.Log($"{gameObject.name} placed on grid slot: {transform.parent?.name ?? "None"}.");
            return; 
        }

        // Updated CheckAndPlaceInHandZone is called here
        if (CheckAndPlaceInHandZone()) 
        {
            Debug.Log($"{gameObject.name} returned to hand by dropping near a hand slot.");
            return; 
        }
        
        Debug.Log($"{gameObject.name} not placed on grid or near a hand slot. Performing fallback return.");
        if (slotOccupiedBeforeDrag != null)  
        {
            Debug.Log($"Attempting to return {gameObject.name} to its original grid slot: {slotOccupiedBeforeDrag.name}.");
            if (slotOccupiedBeforeDrag.PlaceDie(gameObject, this.dieData))
            {
                SetDieVisualsOnParent(slotOccupiedBeforeDrag.transform);
            }
            else 
            {
                Debug.LogError($"Failed to return {gameObject.name} to its original grid slot {slotOccupiedBeforeDrag.name}. Attempting to send to any hand slot.");
                AttemptReturnToAnyAvailableHandSlot(true); 
            }
        }
        else 
        {
            Debug.Log($"{gameObject.name} came from hand and was not placed elsewhere. Returning to an available hand slot.");
            AttemptReturnToAnyAvailableHandSlot(false); 
        }
    }
    // ... (FindClosestGridSlot remains the same) ...
     private GridSlot FindClosestGridSlot()
    {
        GridSlot closest = null;
        float minDistanceSqr = snapDistance * snapDistance;
        if (GridManager.Instance == null || GridManager.Instance.gridSlots == null) return null;
        foreach (GridSlot slot in GridManager.Instance.gridSlots)
        {
            if (slot == null) continue;
            float distanceSqr = (transform.position - slot.transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closest = slot;
            }
        }
        return closest;
    }

    // MODIFIED CheckAndPlaceInHandZone
    private bool CheckAndPlaceInHandZone()
    {
        if (playerHand == null) // playerHand is now essential for this
        {
            Debug.LogError($"[DraggableDie] PlayerHand reference is null on {gameObject.name}. Cannot check for hand slot drop.");
            return false;
        }

        List<Transform> currentHandSlots = playerHand.GetHandSlotTransforms();
        if (currentHandSlots == null || currentHandSlots.Count == 0)
        {
            Debug.LogWarning($"[DraggableDie] No hand slots found via PlayerHand for {gameObject.name}.");
            return false;
        }

        Vector2 dropPosition = GetMouseWorldPos();
        // You could also test with: Vector2 dropPosition = transform.position;

        foreach (Transform slotTransform in currentHandSlots)
        {
            if (slotTransform == null) continue;

            float distanceToSlotCenterSqr = (dropPosition - (Vector2)slotTransform.position).sqrMagnitude;
            // Use the new radius field for this check
            float detectionRadiusSqr = handSlotDropDetectionRadius * handSlotDropDetectionRadius; 

            if (distanceToSlotCenterSqr <= detectionRadiusSqr)
            {
                // Dropped within the radius of a specific hand slot
                Debug.Log($"[DraggableDie] {gameObject.name} IS within radius of hand slot: {slotTransform.name}.");
                bool cameFromGrid = (slotOccupiedBeforeDrag != null);
                
                if (playerHand.ReclaimDieToHand(gameObject, dieData))
                {
                    if (cameFromGrid)
                    {
                        TryRefundMana();
                    }
                    return true; // Successfully reclaimed
                }
                // If ReclaimDieToHand fails (e.g. hand full), we consider this attempt to place in hand zone as failed.
                Debug.LogWarning($"[DraggableDie] Dropped {gameObject.name} near hand slot {slotTransform.name}, but PlayerHand.ReclaimDieToHand FAILED.");
                return false; 
            }
        }
        return false; // Not dropped near any hand slot
    }
    
    // ... (AttemptReturnToAnyAvailableHandSlot, TryRefundMana, SetDieVisualsOnParent, GetMouseWorldPos remain the same) ...
    // Ensure these methods are present from the previous version.
    private void AttemptReturnToAnyAvailableHandSlot(bool potentiallyRefundMana)
    {
        if (playerHand != null)
        {
            bool cameFromGridActualCheck = (slotOccupiedBeforeDrag != null); 
            if (playerHand.ReclaimDieToHand(gameObject, dieData))
            {
                if (potentiallyRefundMana && cameFromGridActualCheck) 
                {
                    TryRefundMana();
                }
                Debug.Log($"{gameObject.name} successfully returned to an available hand slot by PlayerHand.");
            }
            else
            {
                Debug.LogError($"{gameObject.name} could not be returned to any hand slot (e.g., hand full). Die might be left floating.");
            }
        }
        else Debug.LogError($"PlayerHand ref missing on {gameObject.name}. Cannot return to hand.");
    }

    private void TryRefundMana()
    {
        if (CombatManager.Instance != null && CombatManager.Instance.playerStats != null && this.dieData != null)
        {
            if (this.dieData.manaCost > 0)
            {
                CombatManager.Instance.playerStats.GainMana(this.dieData.manaCost);
                Debug.Log($"Refunded {this.dieData.manaCost} mana for returning {this.dieData.name} (from grid) to hand. Player mana AFTER refund: {CombatManager.Instance.playerStats.CurrentMana}");
            }
        }
        else
        {
            Debug.LogError($"Could not attempt mana refund for {this.gameObject.name}: Critical system references missing (CombatManager, PlayerStats, or DieData).");
        }
    }

    private void SetDieVisualsOnParent(Transform newParent)
    {
        transform.SetParent(newParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private Vector3 GetMouseWorldPos()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(this.startWorldPosition).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
    
}