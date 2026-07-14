using System;
using System.Collections.Generic;
using Combinations;

public class AIData
{
    // --- Поля зрения  ---
    public List<CardData> HandCards { get; private set; }
    public List<CardData> TableCards { get; private set; }

    // --- Состояние текущей фазы и ставок ---
    public RoundCombinationSet RoundCombinations { get; private set; }
    public int CurrentParticipationPrice { get; private set; }
    public int OwnCommittedValue { get; private set; }
    public DeclaredCombinationTier? OwnTarget { get; private set; }

    public AIData()
    {
        ClearForNewRound();
    }

    // --- Методы обновления карт игрока--

    /// Полностью заменяет список карт на руке ИИ.
    public void UpdateHandCardsList(List<CardData> newCards)
    {
        if (newCards == null)
        {
            HandCards = new List<CardData>();
            return;
        }

        HandCards = new List<CardData>(newCards);
    }

    /// Добавляет карту в руку ИИ. Если передана конкретная карта — она записывается.
    /// Если передан null — список увеличивается на один пустой слот (для учета скрытых доборов).
    public void AddHandCard(CardData card = null)
    {
        if (card != null)
        {
            HandCards.Add(card);
        }
        else
        {
            HandCards.Add(null);
        }
    }

    /// Вызывается когда открывается новая карта на столе.
    public void AddTableCard(CardData card)
    {
        if (card == null) return;
        TableCards.Add(card);
    }

    /// Обновить информацию о текущем состоянии ставок и фазы перед принятием решения.
    public void UpdateBettingInfo(
        int currentParticipationPrice)
    {
        CurrentParticipationPrice = currentParticipationPrice;
    }

    public void UpdateOwnTarget(DeclaredCombinationTier declaredCombinationTier)
    {
        OwnTarget = declaredCombinationTier;
    }

    public void SetRoundCombinations(RoundCombinationSet roundCombinationSet)
    {
        RoundCombinations = roundCombinationSet;
    }

    /// Очистить изменяемые поля зрения для нового раунда.
    public void ClearForNewRound()
    {
        HandCards = new List<CardData>();
        TableCards = new List<CardData>();

        RoundCombinations = default;
        OwnTarget = null;
        OwnCommittedValue = 0;
        CurrentParticipationPrice = 0;
    }
}
