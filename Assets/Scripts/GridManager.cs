using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public List<GridSlot> gridSlots;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Another instance of GridManager found, destroying this one.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            if (gridSlots == null || gridSlots.Count != 9)
            {
                Debug.LogError("GridManager: Please assign exactly 9 GridSlot objects to the list in the Inspector.", gameObject);
            }
        }
    }

    public GridSlot GetSlotAtIndex(int index)
    {
        if (gridSlots != null && index >= 0 && index < gridSlots.Count)
        {
            return gridSlots[index];
        }
        return null;
    }
}