using System;


[Serializable]
public class CardData : IEquatable<CardData>
{
    private CardSuit suit;
    private CardValue value;
    public CardSuit Suit => suit;
    public CardValue Value => value;
    public CardData(CardSuit suit, CardValue value)
    {
        this.suit = suit;
        this.value = value;
    }

    public bool IsRed => suit == CardSuit.Hearts || suit == CardSuit.Diamonds;
    public bool IsBlack => suit == CardSuit.Clubs || suit == CardSuit.Spades;

    public bool Equals(CardData other)
    {
        return suit == other.Suit && value == other.Value;
    }
    public override bool Equals(object obj)
    {
        return obj is CardData other && Equals(other);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(suit, value);
    }
    public override string ToString()
    {
        return $"{Value} of {Suit}";
    }
}
