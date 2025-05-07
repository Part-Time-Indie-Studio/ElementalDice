// Filename: Scripts/DraggableDie.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableDie : MonoBehaviour
{
    [Header("Dragging Settings")]
    [SerializeField] private float snapDistance = 1.5f;

    private Camera mainCamera;
    private Rigidbody2D rb;

    private Vector3 offset;
    private Vector3 startWorldPosition;

    private bool isDragging = false;
    private PlayerHand playerHand;
    private DieData dieData;

    private Transform parentBeforeDrag;
    private GridSlot slotOccupiedBeforeDrag;
    
    public int CurrentRollValue { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (mainCamera == null) { Debug.LogError("DraggableDie: No camera tagged 'MainCamera' found!"); }
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Initialize(PlayerHand hand, DieData data, Transform startingParent)
    {
        this.playerHand = hand;
        this.dieData = data;
        // Ensure the die is correctly parented to its starting (hand) slot
        transform.SetParent(startingParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
    
        public void SetCurrentRollValue(int rollValue)
        {
            this.CurrentRollValue = rollValue;
        }
    
        public DieData GetDieData()
        {
            return dieData;
        }

    void OnMouseDown()
    {
        if (playerHand == null || mainCamera == null || dieData == null)
        {
            Debug.LogWarning($"DraggableDie on {gameObject.name} is not properly initialized.");
            return;
        }

        isDragging = true;
        rb.isKinematic = true;

        startWorldPosition = transform.position;
        offset = startWorldPosition - GetMouseWorldPos();
        parentBeforeDrag = transform.parent;

        if (parentBeforeDrag != null)
        {
            slotOccupiedBeforeDrag = parentBeforeDrag.GetComponent<GridSlot>();
            if (slotOccupiedBeforeDrag != null && slotOccupiedBeforeDrag.OccupyingDie == gameObject)
            {
                Debug.Log($"Started dragging {gameObject.name} from slot {slotOccupiedBeforeDrag.name}. Removing from slot.");
                slotOccupiedBeforeDrag.RemoveDie();
            }
            else
            {
                slotOccupiedBeforeDrag = null;
                Debug.Log($"Started dragging {gameObject.name} from parent: {parentBeforeDrag.name} (not a recognized occupied grid slot).");
            }
        }
        else
        {
            slotOccupiedBeforeDrag = null;
            Debug.LogWarning($"Started dragging {gameObject.name} which had no parent.");
        }
        transform.SetParent(null);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        Vector3 targetPos = GetMouseWorldPos() + offset;
        rb.MovePosition(targetPos);
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        GridSlot closestSlot = null;
        float minDistanceFoundSqr = snapDistance * snapDistance;

        if (GridManager.Instance == null || GridManager.Instance.gridSlots == null)
        {
            Debug.LogError("GridManager instance or grid slots not found! Attempting to return die to hand.");
            AttemptReturnToHand();
            return;
        }

        foreach (GridSlot slot in GridManager.Instance.gridSlots)
        {
            if (slot == null) continue;
            float distanceSqr = (transform.position - slot.transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceFoundSqr)
            {
                minDistanceFoundSqr = distanceSqr;
                closestSlot = slot;
            }
        }

        bool placedSuccessfullyOnGrid = false;

        if (closestSlot != null)
        {
            if (closestSlot == slotOccupiedBeforeDrag) // Trying to place back into the exact same grid slot
            {
                Debug.Log($"{gameObject.name} is over its original grid slot {closestSlot.name}. Attempting to place back.");
                if (closestSlot.PlaceDie(gameObject, this.dieData))
                {
                    SetDieInSlotVisuals(closestSlot.transform);
                    placedSuccessfullyOnGrid = true;
                    Debug.Log($"{gameObject.name} placed back into its original slot {closestSlot.name}.");
                }
                else
                {
                    Debug.LogWarning($"Failed to place {gameObject.name} back into its original slot {closestSlot.name}.");
                }
            }
            else if (!closestSlot.IsOccupied) // Trying to place in a new, unoccupied grid slot
            {
                Debug.Log($"{gameObject.name} is over a new, unoccupied slot {closestSlot.name}. Attempting to place.");
                if (closestSlot.PlaceDie(gameObject, this.dieData))
                {
                    SetDieInSlotVisuals(closestSlot.transform);
                    placedSuccessfullyOnGrid = true;

                    if (slotOccupiedBeforeDrag == null && playerHand != null) // Came from hand
                    {
                        playerHand.NotifyDiePlacedOnGrid(gameObject);
                        Debug.Log($"{gameObject.name} moved from hand to slot {closestSlot.name}. Notified PlayerHand.");
                    }
                    else // Came from another grid slot
                    {
                        Debug.Log($"{gameObject.name} moved from slot {slotOccupiedBeforeDrag?.name ?? "UnknownSlot"} to new slot {closestSlot.name}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name} failed to place in slot {closestSlot.name} despite it appearing free.");
                }
            }
            else // Closest slot is occupied by another die
            {
                Debug.Log($"Slot {closestSlot.name} is occupied by {closestSlot.OccupyingDie?.name ?? "another die"}. Cannot place {gameObject.name}.");
            }
        }

        if (!placedSuccessfullyOnGrid)
        {
            Debug.Log($"{gameObject.name} was not placed on a grid slot. Attempting to return to hand.");
            AttemptReturnToHand();
        }
        else
        {
            Debug.Log($"Stopped dragging {gameObject.name}. Final Parent: {transform.parent?.name ?? "None"} (On Grid)");
        }
    }

    private void SetDieInSlotVisuals(Transform slotTransform)
    {
        transform.SetParent(slotTransform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity; // Good practice to reset rotation
        transform.localScale = Vector3.one; // And scale
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void AttemptReturnToHand()
    {
        if (playerHand != null)
        {
            if (playerHand.ReclaimDieToHand(gameObject, dieData))
            {
                Debug.Log($"{gameObject.name} successfully returned to hand by PlayerHand.");
                // PlayerHand.ReclaimDieToHand handles parenting and visuals.
            }
            else
            {
                Debug.LogError($"{gameObject.name} could not be returned to hand (e.g., hand full). Die left at current position: {transform.position}. Parent: {transform.parent?.name ?? "None"}");
                // As a last resort, try to parent it to where it was before the drag if it was a hand slot,
                // but this is risky if PlayerHand couldn't reclaim it.
                // For now, it might be left floating if ReclaimDieToHand fails.
                // Or, we could force it back to parentBeforeDrag if it's a hand slot
                if (parentBeforeDrag != null && parentBeforeDrag.GetComponent<GridSlot>() == null) {
                    transform.SetParent(parentBeforeDrag);
                    transform.localPosition = Vector3.zero;
                    Debug.LogWarning($"{gameObject.name} fallback: returned to its direct parentBeforeDrag ({parentBeforeDrag.name}) as PlayerHand couldn't reclaim.");
                } else {
                     Debug.LogWarning($"{gameObject.name} could not be returned to hand and previous parent was a grid slot or null. Die may be floating.");
                }
            }
        }
        else
        {
            Debug.LogError($"PlayerHand reference missing on {gameObject.name}. Cannot return die to hand automatically. Die left at current position.");
        }
        Debug.Log($"Stopped dragging {gameObject.name}. Final Parent: {transform.parent?.name ?? "None"} (Attempted Hand Return)");
    }

    private Vector3 GetMouseWorldPos()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(this.startWorldPosition).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}