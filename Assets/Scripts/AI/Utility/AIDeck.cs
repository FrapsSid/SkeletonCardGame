using System;
using System.Collections.Generic;
using UnityEngine;

public class AIDeck
{
    private List<CardData> _cards = new List<CardData>();
    private System.Random _random = new System.Random();

    public int CardsCount => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;

    public AIDeck()
    {
        GenerateStandardDeck();
    }

    /// Генерирует стандартную колоду из 52 карт.
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

    public void ReturnCard(CardData card)
    {
        _cards.Add(card);
    }

    /// Достает СЛУЧАЙНУЮ карту из колоды по случайному индексу.
    public CardData DrawCard()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("Виртуальная колода пуста! Невозможно взять карту.");
            return null;
        }

        int randomIndex = _random.Next(0, _cards.Count);
        CardData drawnCard = _cards[randomIndex];

        _cards.RemoveAt(randomIndex);

        return drawnCard;
    }

    /// Достает указанное количество случайных карт из колоды.
    public List<CardData> DrawCards(int count)
    {
        List<CardData> drawnCards = new List<CardData>();

        if (count <= 0) return drawnCards;

        for (int i = 0; i < count; i++)
        {
            if (IsEmpty) break;
            drawnCards.Add(DrawCard());
        }

        return drawnCards;
    }

    /// Убирает конкретную карту из колоды по совпадению масти и значения.
    public bool RemoveCard(CardData targetCard)
    {
        if (targetCard == null) return false;

        int index = _cards.FindIndex(c => c.Suit == targetCard.Suit && c.Value == targetCard.Value);

        if (index >= 0)
        {
            _cards.RemoveAt(index);
            return true;
        }

        return false;
    }

    /// Убирает из колоды список известных карт.
    public void RemoveCards(List<CardData> cardsToRemove)
    {
        if (cardsToRemove == null) return;

        foreach (var card in cardsToRemove)
        {
            RemoveCard(card);
        }
    }

    /// Полный сброс и синхронизация колоды. Исключает все карты, которые бот видит.
    public void SyncWithAIData(AIData aiData)
    {
        if (aiData == null) return;

        RemoveCards(aiData.HandCards);
        RemoveCards(aiData.TableCards);
        RemoveCards(aiData.AllyCards);
    }
}
