using System;
using UnityEngine;


[Serializable]
public class CardData
{
    [SerializeField] private CardSuit suit;
    [SerializeField] private CardValue value;
    public CardSuit Suit => suit;
    public CardValue Value => value;
    public CardData(CardSuit suit, CardValue value)
    {
        this.suit = suit;
        this.value = value;
    }
}
