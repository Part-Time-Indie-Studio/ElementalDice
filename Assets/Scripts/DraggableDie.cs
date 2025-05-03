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
    private Transform originalParent;

    private bool isDragging = false;
    private PlayerHand playerHand;
    private DieData dieData;
    private bool isPlaced = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        if (mainCamera == null) { Debug.LogError("DraggableDie: No camera tagged 'MainCamera' found!"); }
        if (rb.bodyType != RigidbodyType2D.Kinematic) { Debug.LogWarning($"DraggableDie on {gameObject.name} should likely be Kinematic.", gameObject); }

    }

    public void Initialize(PlayerHand hand, DieData data, Transform startingParent)
    {
        this.playerHand = hand;
        this.dieData = data;
        this.originalParent = startingParent;
        this.isPlaced = false;
    }

    void OnMouseDown()
    {
        if (isPlaced || playerHand == null || mainCamera == null) return;

        isDragging = true;
        startWorldPosition = transform.position;
        offset = startWorldPosition - GetMouseWorldPos();
        transform.SetParent(null);
        Debug.Log($"Started dragging {gameObject.name}...");
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
        float minDistanceFound = snapDistance;

        Vector3 currentPosition = transform.position;

        if (GridManager.Instance == null || GridManager.Instance.gridSlots == null)
        {
            Debug.LogError("GridManager instance or grid slots not found!");
        }
        else 
        {
            foreach (GridSlot slot in GridManager.Instance.gridSlots)
            {
                if (slot == null) continue;

                float distance = Vector3.Distance(currentPosition, slot.transform.position);

                if (distance < minDistanceFound)
                {
                    minDistanceFound = distance;
                    closestSlot = slot;
                }
            }
        }

        bool droppedOnValidTarget = false;

        if (closestSlot != null && !closestSlot.IsOccupied)
        {

            if (closestSlot.PlaceDie(gameObject, this.dieData))
            {
                droppedOnValidTarget = true;
                isPlaced = true;

                transform.SetParent(closestSlot.transform);
                transform.localPosition = Vector3.zero;

                playerHand.NotifyDiePlacedOnGrid(gameObject);
                Debug.Log($"{gameObject.name} successfully placed in grid slot {closestSlot.name} by distance check.");
            }
            else           
            {
                Debug.LogWarning($"{gameObject.name} failed to place in supposedly unoccupied slot {closestSlot.name}. Returning to hand.");
            }
        }
        else if (closestSlot != null && closestSlot.IsOccupied)
        {
            Debug.Log($"Slot {closestSlot.name} was closest, but is occupied by {closestSlot.OccupyingDie?.name ?? "something"}. Returning to hand.");
        }


        if (!droppedOnValidTarget)
        {
            Debug.Log($"{gameObject.name} not placed on grid (no suitable slot found within {snapDistance} units), returning to hand slot.");
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }

        Debug.Log($"Stopped dragging {gameObject.name}. Placed: {isPlaced}, Parent: {transform.parent?.name ?? "None"}");
    }


    private Vector3 GetMouseWorldPos()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(startWorldPosition).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}