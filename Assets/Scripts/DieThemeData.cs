using System;
using UnityEngine;
using TMPro; 
using System.Collections.Generic;


[Serializable]
public class ElementVisualMapping
{
    public DieElement element;
    public Color backgroundColor = Color.white; // Or Material if you prefer
}

[Serializable]
public class ActionVisualMapping
{
    public DieActionType actionType;
    public Sprite actionIcon;
}

[Serializable]
public class RarityVisualMapping
{
    public DieRarity rarity;
    public Color rarityColor = Color.white;
}

[Serializable]
public class DieShapePrefabMapping
{
    public DieSides sides;
    public GameObject diePrefab;
}


[CreateAssetMenu(fileName = "DieTheme", menuName = "YourGame/Die Visual Theme")]
public class DieThemeData : ScriptableObject
{
    [Header("Element Visuals")]
    // Use lists + structs for better Inspector editing than dictionaries sometimes
    public List<ElementVisualMapping> elementVisuals;
    public Color defaultBackgroundColour; // Fallback

    [Header("Action Icons")]
    public List<ActionVisualMapping> actionVisuals;
    public Sprite defaultActionIcon; // Fallback

    [Header("Rarity Indicators")]
    public List<RarityVisualMapping> rarityVisuals;
    public Color defaultRarityColor = Color.grey;

    [Header("Fonts & Text Colors")]
    public TMP_FontAsset defaultFont; // Or Font for standard UI Text
    public Color manaTextColor = Color.black;


    [Header("Die Shape Prefabs")]
    public List<DieShapePrefabMapping> shapePrefabs;
    public GameObject defaultDiePrefab;

    public Color GetElementColor(DieElement element)
    {
        foreach (var mapping in elementVisuals)
        {
            if (mapping.element == element) return mapping.backgroundColor;
        }
        // Find default/None mapping or return a default color
        foreach (var mapping in elementVisuals)
        {
            if (mapping.element == DieElement.None) return mapping.backgroundColor;
        }
        return defaultBackgroundColour; // Fallback
    }

    public Sprite GetActionIcon(DieActionType actionType)
    {
        foreach (var mapping in actionVisuals)
        {
            if (mapping.actionType == actionType) return mapping.actionIcon;
        }
        return defaultActionIcon; // Fallback
    }

    public Color GetRarityColor(DieRarity rarity)
    {
        foreach (var mapping in rarityVisuals)
        {
            if (mapping.rarity == rarity) return mapping.rarityColor;
        }
        return defaultRarityColor; // Fallback
    }


    public GameObject GetPrefabForSides(DieSides sides)
    {
        foreach (var mapping in shapePrefabs)
        {
            if (mapping.sides == sides)
            {
                // Ensure prefab is actually assigned in the Inspector
                if (mapping.diePrefab != null)
                {
                    return mapping.diePrefab;
                }
                else
                {
                    Debug.LogWarning($"Prefab not assigned for DieSides: {sides} in Theme: {this.name}");
                }
            }
        }
        Debug.LogWarning($"No specific prefab found for DieSides: {sides}. Returning default.");
        return defaultDiePrefab; // Return default if specific not found or assigned
    }
}