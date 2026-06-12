using System;
using System.Collections.Generic;
using UnityEngine;

public class Hand
{
    private readonly List<CardData> _cards = new List<CardData>();
    private int _maxCardsLimit;

    public event Action<List<CardData>> OnHandChanged;

    public int CurrentCount => _cards.Count;
    public int MaxCardsLimit => _maxCardsLimit;
    public bool IsFull => _cards.Count >= _maxCardsLimit;

    public Hand(int maxCardsLimit)
    {
        _maxCardsLimit = maxCardsLimit;
    }

    public bool AddCard(CardData card)
    {
        if (card == null) return false;

        if (IsFull)
        {
            Debug.LogWarning($"Cannot add card! The skeleton hand limit ({_maxCardsLimit}) has been reached.");
            return false;
        }

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

    public void DecreaseMaxCardsLimit(int amount)
    {
        if (amount <= 0) return;

        _maxCardsLimit = Mathf.Max(0, _maxCardsLimit - amount);

        bool wasModified = false;
        while (_cards.Count > _maxCardsLimit)
        {
            int lastIndex = _cards.Count - 1;

            _cards.RemoveAt(lastIndex);
            wasModified = true;
        }

        if (wasModified)
        {
            OnHandChanged?.Invoke(new List<CardData>(_cards));
        }
    }

    public void IncreaseMaxCardsLimit(int amount)
    {
        if (amount <= 0) return;

        _maxCardsLimit += amount;
    }
}
