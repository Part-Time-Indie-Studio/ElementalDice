using UnityEngine;

public class GridSlot : MonoBehaviour
{
    public bool IsOccupied { get; private set; } = false;
    public GameObject OccupyingDie { get; private set; } = null;
    public DieData OccupyingDieData { get; private set; } = null;

    public bool PlaceDie(GameObject dieInstance, DieData dieData)
    {
        if (!IsOccupied)
        {
            IsOccupied = true;
            OccupyingDie = dieInstance;
            OccupyingDieData = dieData;
            Debug.Log($"Grid slot {gameObject.name} occupied by {dieInstance.name}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Grid slot {gameObject.name} is already occupied by {OccupyingDie.name}. Cannot place {dieInstance.name}.");
            return false;
        }
    }

    public void RemoveDie()
    {
        if (IsOccupied)
        {
            Debug.Log($"Die {OccupyingDie.name} removed from grid slot {gameObject.name}");
            IsOccupied = false;
            OccupyingDie = null;
            OccupyingDieData = null;
        }
    }
}