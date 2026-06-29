using System;

namespace Combinations
{
    /// <summary>
    /// Карта с информацией об источнике (пуле)
    /// </summary>
    public struct CardWithPool : IEquatable<CardWithPool>
    {
        public CardData Card { get; }
        public CardPool Pool { get; }

        public CardWithPool(CardData card, CardPool pool)
        {
            Card = card;
            Pool = pool;
        }

        public CardWithPool(CardSuit suit, CardValue value, CardPool pool)
        {
            Card = new CardData(suit, value);
            Pool = pool;
        }

        // Shortcuts для удобства
        public CardSuit Suit => Card.Suit;
        public CardValue Value => Card.Value;
        public bool IsRed => Card.IsRed;
        public bool IsBlack => Card.IsBlack;

        public bool Equals(CardWithPool other)
        {
            return Card.Equals(other.Card) && Pool == other.Pool;
        }

        public override bool Equals(object obj)
        {
            return obj is CardWithPool other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Card, Pool);
        }

        public override string ToString()
        {
            return $"{Card} ({Pool})";
        }
    }
}
