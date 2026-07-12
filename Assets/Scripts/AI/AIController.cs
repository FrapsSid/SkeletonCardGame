using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardGame;

public class AIController
{
    // --- Поля архитектуры ИИ ---
    public Skeleton player { get; private set; }
    private AIData _aiData;
    private BaseAIDecisionStrategy _decisionStrategy;

    private TestAiTurnAdapter aiTurnAdapter;

    /// Инициализация контроллера ИИ. Сюда передается конкретная стратегия (Эвристика или MCTS).
    public AIController(BaseAIDecisionStrategy strategy, TestAiTurnAdapter testAiTurnAdapter, Skeleton player)
    {
        _decisionStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        aiTurnAdapter = testAiTurnAdapter;

        this.player = player;

        _aiData = new AIData();
    }


    /// Внутренний рабочий цикл размышления бота.
    public void ExecuteTurn(Round round)
    {
        if (round == null || round.CurrentPlayer != player)
            return;

        _aiData.UpdateBettingInfo(round.currentParticipationPrice);

        _aiData.SetRoundCombinations(round.Combinations);

        if(round.playerStates[player].declaredTarget.HasValue)
            _aiData.UpdateOwnTarget(round.playerStates[player].declaredTarget.Value);

        GameStateSnapshot snapshot = CreateSnapshotFromAIData();

        AIResponsePackage cardActionResponse;

        if (!round.HasMatchedBet(player))
        {
            AIResponsePackage bettingResponse = _decisionStrategy.ChooseBettingAction(snapshot);

            aiTurnAdapter.ExecuteBettingAction(bettingResponse, player);

            if (bettingResponse.Action != AIActionType.Fold)
            {
                cardActionResponse = _decisionStrategy.ChooseCardAction(snapshot);

                aiTurnAdapter.ExecuteCardAction(cardActionResponse, player);
            }
        } else
        {
            cardActionResponse = _decisionStrategy.ChooseCardAction(snapshot);

            aiTurnAdapter.ExecuteCardAction(cardActionResponse, player);
        }  
    }

    /// Создает и полностью детерминирует GameStateSnapshot на основе данных из AIData.
    /// Известные карты копируются, неизвестные (null) — случайным образом заполняются из колоды.
    private GameStateSnapshot CreateSnapshotFromAIData()
    {
        // 1. Инициализируем чистый объект снимка состояния
        GameStateSnapshot snapshot = new GameStateSnapshot();

        // 2. Переносим все базовые открытые параметры раунда и стола прямиком из AIData
        snapshot.RoundCombinations = _aiData.RoundCombinations;

        // Ссылки на физическое тело и заявку текущего бота (Own)
        snapshot.OwnBody = player.Body;
        snapshot.OwnTarget = _aiData.OwnTarget;
        snapshot.OwnCommittedValue = _aiData.OwnCommittedValue;

        // Переносим общие карты стола (создавая новый независимый список)
        snapshot.TableCards = new List<CardData>(_aiData.TableCards);

        snapshot.CurrentParticipationPrice = _aiData.CurrentParticipationPrice;

        AIDeck aIDeck = new AIDeck();

        aIDeck.SyncWithAIData(_aiData);

        snapshot._AIDeck = aIDeck;

        // --- 4. ДЕТЕРМИНИЗАЦИЯ РУК ИГРОКОВ (Заполнение скрытых зон) ---
        snapshot.OwnHand = FillHand(_aiData.HandCards, snapshot._AIDeck);

        // snapshot.Enemy1Hand = FillHand(new List<CardData>(Enemy 1 hand count), snapshot._AIDeck);
        // snapshot.Enemy1Hand = FillHand(new List<CardData>(Enemy 2 hand count), snapshot._AIDeck);

        // Возвращаем полностью собранный, детерминированный и безопасный слепок игры "от первого лица"
        return snapshot;
    }

    private List<CardData> FillHand(List<CardData> sourceCards, AIDeck virtualDeck)
    {
        List<CardData> generatedHand = new List<CardData>();

        if (sourceCards == null) return generatedHand;

        for (int i = 0; i < sourceCards.Capacity; i++)
        {
            // Проверяем, есть ли известная карта по текущему индексу
            CardData knownCard = (i < sourceCards.Count) ? sourceCards[i] : null;

            if (knownCard != null)
            {
                generatedHand.Add(knownCard);
            }
            else
            {
                // Карта скрыта — берем случайную из виртуальной колоды
                generatedHand.Add(virtualDeck.DrawCard());
            }
        }

        return generatedHand;
    }


    public void OnHandCardDealt(CardData card)
    {
        _aiData.AddHandCard(card);
    }

    public void OnTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        foreach(CardData card in cards)
        {
            _aiData.AddTableCard(card);
        }
    }

    public void OnRoundEnded()
    {
        _aiData.ClearForNewRound();
    }
}
