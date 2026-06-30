using System;
using System.Collections.Generic;

namespace Combinations.Tests
{
    [Serializable]
    public class CombinationTestConfig
    {
        public List<CombinationTestCase> testCases;
    }

    [Serializable]
    public class CombinationTestCase
    {
        public string testCaseId;
        public string combinationType; // Название класса комбинации
        public string description;
        public bool shouldPass; // true = должен пройти, false = должен провалиться
        public List<TestCard> cards;
    }

    [Serializable]
    public class TestCard
    {
        public string suit; // "Hearts", "Diamonds", "Clubs", "Spades"
        public string value; // "Two", "Three", ..., "King", "Ace"
        public string pool; // "Player1Hand", "Player2Hand", "Table"

        public CardWithPool ToCardWithPool()
        {
            CardSuit cardSuit = (CardSuit)Enum.Parse(typeof(CardSuit), suit);
            CardValue cardValue = (CardValue)Enum.Parse(typeof(CardValue), value);
            CardPool cardPool = (CardPool)Enum.Parse(typeof(CardPool), pool);

            return new CardWithPool(cardSuit, cardValue, cardPool);
        }
    }
}