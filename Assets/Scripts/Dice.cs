using UnityEngine;

public enum DieSides
{
    D4 = 4,
    D6 = 6,
    D8 = 8,
    D10 = 10
}

public enum DieRarity
{
    Common,
    Uncommon,
    Rare,
    Mythic
}

public enum DieActionType
{
    Attack,
    Block,
    Heal
}

public enum DieElement
{
    None,     // For non-elemental / physical actions
    Fire,     // Color: Red
    Earth,    // Color: Brown
    Air,      // Color: Grey
    Water     // Color: Blue
}

public enum TargetType
{
    Self,
    SingleEnemy
}


[CreateAssetMenu(fileName = "NewDie", menuName = "YourGame/Die Definition")]
public class DieData : ScriptableObject
{
    [Header("Identification")]
    public string dieID;

    [Header("Core Stats")] 
    public DieSides sides = DieSides.D6;
    public DieRarity rarity = DieRarity.Common;
    public int manaCost = 1;
    public DieElement element = DieElement.None;

    [Header("Action Details")]
    public DieActionType actionType = DieActionType.Attack;
    public TargetType targetType = TargetType.SingleEnemy;

    public int GetMaxRollValue()
    {
        return (int)sides;
    }


    private void OnEnable()
    {
#if UNITY_EDITOR // Only run this code in the editor
        if (string.IsNullOrEmpty(dieID))
        {
            dieID = System.Guid.NewGuid().ToString();
            // Optional: Mark the object as dirty so Unity saves the new ID
             UnityEditor.EditorUtility.SetDirty(this); 
        }
#endif
    }
}