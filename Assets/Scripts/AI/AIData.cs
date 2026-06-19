using System;
using System.Collections.Generic;

public class AIData
{
    // --- Ссылка на физическое тело ИИ-скелета ---
    public SkeletonBody Body { get; private set; }

    // --- Поля зрения (карты и состояние стола) ---
    public List<CardData> HandCards { get; private set; }
    public List<CardData> AllyVisibleCards { get; private set; }
    public List<CardData> TableCards { get; private set; }
    public List<CardData> Enemy1Cards { get; private set; }
    public List<CardData> Enemy2Cards { get; private set; }

    // --- Состояние текущей фазы и ставок ---
    public RoundCombinationSet RoundCombinations { get; private set; }
    public int CurrentParticipationPrice { get; private set; }
    public DeclaredCombinationTier? OwnDeclaredTarget { get; private set; }
    public int OwnCommittedValue { get; private set; }
    public int CurrentIteration { get; private set; }
    public AIDeck _AIDeck { get; private set; }

    /// <summary>
    /// Конструктор AIData. Требует обязательную ссылку на компонент SkeletonBody.
    /// </summary>
    public AIData(SkeletonBody body)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body), "ИИ-данные не могут быть созданы без ссылки на SkeletonBody.");

        ClearForNewRound();
    }

    // --- Методы обновления карт игрока--

    /// <summary>
    /// Полностью заменяет список карт на руке ИИ, создавая абсолютно новую независимую копию.
    /// </summary>
    public void UpdateHandCardsList(List<CardData> newCards)
    {
        if (newCards == null)
        {
            HandCards = new List<CardData>();
            return;
        }

        // Создаем новый независимый экземпляр списка на основе переданного
        HandCards = new List<CardData>(newCards);
    }

    /// <summary>
    /// Добавляет карту в руку ИИ. Если передана конкретная карта — она записывается.
    /// Если передан null — список увеличивается на один пустой слот (для учета скрытых доборов).
    /// </summary>
    public void AddHandCard(CardData card = null)
    {
        // Если пришла конкретная карта, просто добавляем её в пул
        if (card != null)
        {
            HandCards.Add(card);
        }
        else
        {
            // Если передан null, мы расширяем список на 1 элемент, добавляя null.
            // Это позволяет ИИ точно знать, сколько карт у него на руке, даже если сами значения карт скрыты.
            HandCards.Add(null);
        }
    }


    // --- Методы обновления карт союзника (AllyVisibleCards) ---

    /// <summary>
    /// Полностью заменяет видимый список карт напарника, создавая новую независимую копию.
    /// Вызывается только если у скелета ИИ есть голова (HasSkull).
    /// </summary>
    public void UpdateAllyCardsList(List<CardData> newCards)
    {
        if (newCards == null)
        {
            AllyVisibleCards = new List<CardData>();
            return;
        }

        // Создаем новый независимый экземпляр списка на основе переданного
        AllyVisibleCards = new List<CardData>(newCards);
    }

    /// <summary>
    /// Добавляет карту союзника в поле зрения ИИ. 
    /// Если передан null — список расширяется на один пустой слот (ИИ увидел факт добора карты напарником).
    /// Вызывается только если у скелета ИИ есть голова (HasSkull).
    /// </summary>
    public void AddAllyCard(CardData card = null)
    {
        if (card != null)
        {
            AllyVisibleCards.Add(card);
        }
        else
        {
            // Расширяем список на 1 элемент без конкретного значения
            AllyVisibleCards.Add(null);
        }
    }

    /// <summary>
    /// Вызывается когда открывается новая карта на столе.
    /// </summary>
    public void AddTableCard(CardData card)
    {
        if (card == null) return;
        TableCards.Add(card);
    }

    /// <summary>
    /// Обновить информацию о текущем состоянии ставок и фазы перед принятием решения.
    /// </summary>
    public void UpdateBettingInfo(
        int currentPrice,
        DeclaredCombinationTier? ownTarget,
        int ownCommitted,
        int currentIteration,
        RoundCombinationSet currentCombinations)
    {
        CurrentParticipationPrice = currentPrice;
        OwnDeclaredTarget = ownTarget;
        OwnCommittedValue = ownCommitted;
        CurrentIteration = currentIteration;
        RoundCombinations = currentCombinations;
    }

    /// <summary>
    /// Очистить изменяемые поля зрения для нового раунда.
    /// </summary>
    public void ClearForNewRound()
    {
        HandCards = new List<CardData>();
        AllyVisibleCards = new List<CardData>();
        TableCards = new List<CardData>();
        Enemy1Cards = new List<CardData>();
        Enemy2Cards = new List<CardData>();

        _AIDeck = new AIDeck();
        RoundCombinations = default;
        CurrentParticipationPrice = 0;
        OwnDeclaredTarget = null;
        OwnCommittedValue = 0;
        CurrentIteration = 0;
    }
}
