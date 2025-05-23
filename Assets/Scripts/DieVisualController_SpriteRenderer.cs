using UnityEngine;
using TMPro;

public class DieVisualControllerSpriteRenderer : MonoBehaviour
{
    [Header("Theme Data")]
    [SerializeField] private DieThemeData themeData; // Assign your DieThemeData asset here!

    [Header("Component References (SpriteRenderer Based)")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;
    [SerializeField] private SpriteRenderer actionIconSpriteRenderer;
    [SerializeField] private SpriteRenderer rarityIndicatorSpriteRenderer;

    [SerializeField] private TextMeshProUGUI manaCostText;
    [SerializeField] private TextMeshProUGUI rollResultText;

    private DieData _currentDieData; 

    public void DisplayDie(DieData dieData)
    {
        if (dieData == null || themeData == null)
        {
            Debug.LogError("DieData or ThemeData is missing!");
            gameObject.SetActive(false);
            return;
        }

        _currentDieData = dieData;
        gameObject.SetActive(true);

        if (manaCostText)
        {
            manaCostText.text = _currentDieData.manaCost.ToString();
            manaCostText.color = themeData.manaTextColor;
            manaCostText.gameObject.SetActive(true);
        }

        if (backgroundSpriteRenderer)
        {
            backgroundSpriteRenderer.color = themeData.GetElementColor(_currentDieData.element);
        }

        if (actionIconSpriteRenderer)
        {
            actionIconSpriteRenderer.sprite = themeData.GetActionIcon(_currentDieData.actionType);
            actionIconSpriteRenderer.enabled = actionIconSpriteRenderer.sprite != null;
        }
        if (rarityIndicatorSpriteRenderer)
        {
            rarityIndicatorSpriteRenderer.color = themeData.GetRarityColor(_currentDieData.rarity);
        }

        HideRollResult();
    }

    public void ShowRollResult(int result)
    {
        if (rollResultText != null)
        {
            rollResultText.text = result.ToString();
            rollResultText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("RollResultText component is not assigned on " + gameObject.name);
        }
    }

    public void HideRollResult()
    {
        if (rollResultText != null)
        {
            rollResultText.text = ""; 
            rollResultText.gameObject.SetActive(false);
        }
    }
}