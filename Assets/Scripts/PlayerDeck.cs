using UnityEngine;
using System.Collections.Generic; 

public class PlayerDeck : MonoBehaviour
{
    [Header("Deck Setup")]
    [SerializeField] private List<DieData> startingDeckDefinition = new List<DieData>();

    [SerializeField] private List<DieData> drawPile = new List<DieData>();
    [SerializeField] private List<DieData> discardPile = new List<DieData>();

    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;

    private System.Random rng = new System.Random();

    private void Awake()
    {
        InitializeDeck();
    }

    public void InitializeDeck()
    {
        Debug.Log("Initializing Player Deck...");
        drawPile.Clear();
        discardPile.Clear();

        drawPile.AddRange(startingDeckDefinition);

        ShuffleDrawPile();
        Debug.Log($"Deck Initialized. Draw Pile: {DrawPileCount}, Discard Pile: {DiscardPileCount}");
    }

    public void ShuffleDrawPile()
    {
        Debug.Log("Shuffling Draw Pile...");
        int n = drawPile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1); 
            DieData value = drawPile[k];
            drawPile[k] = drawPile[n];
            drawPile[n] = value;
        }
        Debug.Log("Draw Pile Shuffled.");
    }

    public DieData DrawDie()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count > 0)
            {
                Debug.Log("Draw pile empty, reshuffling discard pile...");
                ReshuffleDiscardIntoDraw();
            }
            else
            {
                Debug.LogWarning("Draw and Discard piles are empty. Cannot draw die.");
                return null;
            }
        }

        if (drawPile.Count == 0)
        {
            Debug.LogWarning("Draw pile still empty after attempting reshuffle. Cannot draw die.");
            return null;
        }

        int lastIndex = drawPile.Count - 1;
        DieData drawnDie = drawPile[lastIndex];
        drawPile.RemoveAt(lastIndex);

        Debug.Log($"Drew die: {drawnDie?.name ?? "NULL"}. Draw Pile Remaining: {DrawPileCount}");
        return drawnDie;
    }

    public void DiscardDie(DieData dieData)
    {
        if (dieData != null)
        {
            discardPile.Add(dieData);
            Debug.Log($"Discarded die: {dieData.name}. Discard Pile Size: {DiscardPileCount}");
        }
    }

    public void AddDieToDeck(DieData newDieData)
    {
        if (newDieData != null)
        {
            discardPile.Add(newDieData);
            Debug.Log($"Added new die {newDieData.name} to discard pile.");
        }
    }


    private void ReshuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();            
        ShuffleDrawPile();              
    }
}