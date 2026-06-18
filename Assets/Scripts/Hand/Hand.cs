using System;
using System.Collections.Generic;
using UnityEngine;

public class Hand
{
    private readonly List<CardData> _cards = new List<CardData>();

    public event Action<List<CardData>> OnHandChanged;

    public Hand()
    {
    }

    public bool AddCard(CardData card)
    {
        if (card == null) return false;

        _cards.Add(card);

        OnHandChanged?.Invoke(new List<CardData>(_cards));
        return true;
    }

    public bool RemoveCard(CardData card)
    {
        if (card == null) return false;

        if (!_cards.Contains(card))
        {
            Debug.LogWarning("Cannot remove card: This card is not in the hand!");
            return false;
        }

        _cards.Remove(card);

        OnHandChanged?.Invoke(new List<CardData>(_cards));
        return true;
    }

    public List<CardData> GetCards()
    {
        return new List<CardData>(_cards);
    }
}
