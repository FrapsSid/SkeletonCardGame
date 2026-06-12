using System;
using UnityEngine;


[Serializable]
public class CardData
{
    [SerializeField] private CardSuit suit;
    [SerializeField] private CardValue value;
    [SerializeField] private bool isRevealed;

    public CardSuit Suit => suit;
    public CardValue Value => value;
    public bool IsRevealed
    {
        get => isRevealed;
        set => isRevealed = value;
    }

    public CardData(CardSuit suit, CardValue value, bool isRevealed = false)
    {
        this.suit = suit;
        this.value = value;
        this.isRevealed = isRevealed;
    }
}
