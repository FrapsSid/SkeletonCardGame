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

    /// <summary>
    /// Генерирует стандартную колоду из 52 карт.
    /// </summary>
    private void GenerateStandardDeck()
    {
        _cards.Clear();

        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
            {
                _cards.Add(new CardData(suit, value, false));
            }
        }
    }

    public void ReturnCard(CardData card)
    {
        _cards.Add(card);
    }

    /// <summary>
    /// Достает СЛУЧАЙНУЮ карту из колоды по случайному индексу (вместо шафла).
    /// </summary>
    public CardData DrawCard()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("Виртуальная колода пуста! Невозможно взять карту.");
            return null;
        }

        // Выбираем случайный индекс в пределах текущего размера колоды
        int randomIndex = _random.Next(0, _cards.Count);
        CardData drawnCard = _cards[randomIndex];

        // Удаляем карту из этой позиции, чтобы она не выпала снова
        _cards.RemoveAt(randomIndex);

        return drawnCard;
    }

    /// <summary>
    /// Достает указанное количество случайных карт из колоды.
    /// </summary>
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

    /// <summary>
    /// Убирает конкретную карту из колоды по совпадению масти и значения.
    /// </summary>
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

    /// <summary>
    /// Убирает из колоды список известных карт.
    /// </summary>
    public void RemoveCards(List<CardData> cardsToRemove)
    {
        if (cardsToRemove == null) return;

        foreach (var card in cardsToRemove)
        {
            RemoveCard(card);
        }
    }

    /// <summary>
    /// Полный сброс и синхронизация колоды. Исключает все карты, которые бот видит.
    /// </summary>
    public void SyncWithAIData(AIData aiData)
    {
        if (aiData == null) return;

        RemoveCards(aiData.HandCards);
        RemoveCards(aiData.TableCards);
        RemoveCards(aiData.AllyVisibleCards);
    }
}
