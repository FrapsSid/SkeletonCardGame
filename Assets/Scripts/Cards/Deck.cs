using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<CardData> _cards = new List<CardData>();
    private System.Random _random = new System.Random();

    public int CardsCount => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;

    public Deck()
    {
        Reset();
    }

    private void GenerateStandardDeck()
    {
        _cards.Clear();

        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
            {
                _cards.Add(new CardData(suit, value));
            }
        }
    }

    public void Shuffle()
    {
        int n = _cards.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            CardData value = _cards[k];
            _cards[k] = _cards[n];
            _cards[n] = value;
        }
    }

    public CardData DrawCard()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("Cannot draw a card: The deck is empty!");
            return null;
        }

        int topCardIndex = _cards.Count - 1;
        CardData drawnCard = _cards[topCardIndex];
        _cards.RemoveAt(topCardIndex);

        return drawnCard;
    }

    public List<CardData> DrawCards(int count)
    {
        List<CardData> drawnCards = new List<CardData>();

        if (count <= 0) return drawnCards;

        for (int i = 0; i < count; i++)
        {
            if (IsEmpty)
            {
                Debug.LogWarning($"Requested {count} cards, but only {drawnCards.Count} were available.");
                break;
            }
            drawnCards.Add(DrawCard());
        }

        return drawnCards;
    }

    public void ReturnCard(CardData card)
    {
        if (card == null) return;

        _cards.Insert(0, card);
    }

    public void Reset()
    {
        GenerateStandardDeck();
        Shuffle();
    }
}
