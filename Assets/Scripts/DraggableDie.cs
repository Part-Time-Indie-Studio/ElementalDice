using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableDie : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Collider2D col;

    private Vector3 offset;
    private Vector3 startPosition; // To remember where it started
    private Transform originalParent; // To remember which hand slot it was in

    private bool isDragging = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("DraggableDie: No camera tagged 'MainCamera' found in the scene!");
        }
        // Ensure Rigidbody is set to Kinematic if we never want physics forces
        if (rb.bodyType != RigidbodyType2D.Kinematic)
        {
            Debug.LogWarning($"DraggableDie on {gameObject.name} has Rigidbody type {rb.bodyType}. Consider setting to Kinematic if no physics interactions are desired.", gameObject);
        }
    }

    void OnMouseDown()
    {
        Debug.Log(isDragging);
        if (mainCamera == null) return;

        isDragging = true;
        startPosition = transform.position; // Record world position
        originalParent = transform.parent; // Record original parent (the hand slot)

        offset = startPosition - GetMouseWorldPos(); // Calculate offset based on world position

        // Temporarily unparent so dragging is smooth and independent of parent
        transform.SetParent(null);


        Debug.Log($"Started dragging {gameObject.name} from parent {originalParent?.name ?? "None"}");
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null) return;

        Vector3 targetPos = GetMouseWorldPos() + offset;
        rb.MovePosition(targetPos); // Use MovePosition for Kinematic bodies too
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;
        Debug.Log($"Stopped dragging {gameObject.name}");

        // --- Valid Drop Check Placeholder ---
        // In the future, we will check here if the die is over a valid grid slot.
        bool droppedOnValidTarget = false; // Hardcoded to false for now

        if (droppedOnValidTarget)
        {
            // If it WAS dropped on a valid target (e.g., a grid slot)
            // We'll add logic here later. For example, setting its new parent
            // to be the grid slot.
            Debug.Log($"{gameObject.name} dropped on a valid target (logic not implemented yet).");
            // Example: transform.SetParent(theGridSlotTransform);
            // Example: transform.localPosition = Vector3.zero;
        }
        else
        {
            // If not dropped on a valid target, return to original slot
            Debug.Log($"{gameObject.name} not dropped on valid target, returning to hand slot.");
            transform.SetParent(originalParent); // Re-attach to the original hand slot
            transform.localPosition = Vector3.zero; // Reset position within the slot
                                                    // transform.position = startPosition; // Alternative: directly reset world position
                                                    // Setting localPosition is usually better after parenting
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        // Use the stored startPosition's Z coordinate if unparenting causes Z issues
        // mousePoint.z = mainCamera.WorldToScreenPoint(startPosition).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}