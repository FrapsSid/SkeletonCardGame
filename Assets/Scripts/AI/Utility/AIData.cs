using System;
using System.Collections.Generic;
using Combinations;

public class AIData
{
    public SkeletonBody OwnBody { get; private set; }

    // --- Поля зрения (карты и состояние стола) ---
    public List<CardData> HandCards { get; private set; }
    public List<CardData> AllyCards { get; private set; }
    public List<CardData> TableCards { get; private set; }

    // --- Состояние текущей фазы и ставок ---
    public RoundCombinationSet RoundCombinations { get; private set; }
    public int CurrentParticipationPrice { get; private set; }
    public int PotSize { get; private set; }
    public int OwnCommittedValue { get; private set; }
    public DeclaredCombinationTier? OwnTarget { get; private set; }
    public DeclaredCombinationTier? AllyTarget { get; private set; }
    public DeclaredCombinationTier? Enemy1Target { get; private set; }
    public DeclaredCombinationTier? Enemy2Target { get; private set; }
    public int CurrentIteration { get; private set; }
    public AIDeck _AIDeck { get; private set; }

    public AIData(SkeletonBody body)
    {
        OwnBody = body ?? throw new ArgumentNullException(nameof(body), "ИИ-данные не могут быть созданы без ссылки на SkeletonBody.");

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


    // --- Методы обновления карт союзника (AllyVisibleCards) ---

    /// Полностью заменяет видимый список карт напарника.
    public void UpdateAllyCardsList(List<CardData> newCards)
    {
        if (newCards == null)
        {
            AllyCards = new List<CardData>();
            return;
        }

        AllyCards = new List<CardData>(newCards);
    }

    /// Добавляет карту союзника в поле зрения ИИ. 
    /// Если передан null — список расширяется на один пустой слот (ИИ увидел факт добора карты напарником).
    public void AddAllyCard(CardData card = null)
    {
        if (card != null)
        {
            AllyCards.Add(card);
        }
        else
        {
            AllyCards.Add(null);
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
        int currentPrice,
        int potSize,
        DeclaredCombinationTier? ownTarget,
        DeclaredCombinationTier? allyTarget,
        DeclaredCombinationTier? enemy1arget,
        DeclaredCombinationTier? enemy2Target,
        int ownCommitted,
        int currentIteration,
        RoundCombinationSet currentCombinations)
    {
        CurrentParticipationPrice = currentPrice;
        PotSize = potSize;
        OwnTarget = ownTarget;
        AllyTarget = allyTarget;
        Enemy1Target = enemy1arget;
        Enemy2Target = enemy2Target;
        OwnCommittedValue = ownCommitted;
        CurrentIteration = currentIteration;
        RoundCombinations = currentCombinations;
    }

    /// Очистить изменяемые поля зрения для нового раунда.
    public void ClearForNewRound()
    {
        HandCards = new List<CardData>();
        AllyCards = new List<CardData>();
        TableCards = new List<CardData>();

        _AIDeck = new AIDeck();
        RoundCombinations = default;
        CurrentParticipationPrice = 0;
        OwnTarget = null;
        AllyTarget = null;
        Enemy1Target = null;
        Enemy2Target = null;
        OwnCommittedValue = 0;
        CurrentIteration = 0;
        PotSize = 0;
    }
}
