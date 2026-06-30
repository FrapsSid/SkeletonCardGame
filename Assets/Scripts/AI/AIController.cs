using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    // --- Поля архитектуры ИИ ---
    private AIData _aiData;
    private BaseAIDecisionStrategy _decisionStrategy;

    private bool _isThinking;

    /// Инициализация контроллера ИИ. Сюда передается конкретная стратегия (Эвристика или MCTS).
    public void Initialize(BaseAIDecisionStrategy strategy)
    {
        _decisionStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        _aiData = new AIData(null);

        _isThinking = false;

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// Главный метод выполнения хода. Вызывается Игровым Менеджером, когда наступает очередь этого бота.
    public void ExecuteTurn()
    {
    }

    /// Внутренний рабочий цикл размышления бота.
    private IEnumerator ExecuteTurnRoutine(bool isBettingPhase)
    {
        _isThinking = true;

        _isThinking = false;

        yield break;
    }

    /// Создает и полностью детерминирует GameStateSnapshot на основе данных из AIData.
    /// Известные карты копируются, неизвестные (null) — случайным образом заполняются из колоды.
    private GameStateSnapshot CreateSnapshotFromAIData()
    {
        // 1. Инициализируем чистый объект снимка состояния
        GameStateSnapshot snapshot = new GameStateSnapshot();

        // 2. Переносим все базовые открытые параметры раунда и стола прямиком из AIData
        snapshot.CurrentIteration = _aiData.CurrentIteration;
        snapshot.RoundCombinations = _aiData.RoundCombinations;
        snapshot.CurrentParticipationPrice = _aiData.CurrentParticipationPrice;

        // Ссылки на физическое тело и заявку текущего бота (Own)
        snapshot.OwnBody = _aiData.OwnBody;
        snapshot.OwnTarget = _aiData.OwnTarget;
        snapshot.OwnCommittedValue = _aiData.OwnCommittedValue;

        // Переносим общие карты стола (создавая новый независимый список)
        snapshot.TableCards = new List<CardData>(_aiData.TableCards);

        snapshot.PotSize = _aiData.PotSize;
        snapshot.AllyTarget = _aiData.AllyTarget;
        snapshot.Enemy1Target = _aiData.Enemy1Target;
        snapshot.Enemy2Target = _aiData.Enemy2Target;

        // 3. Синхронизируем симуляционную колоду ИИ с текущими известными данными стола
        // Метод SyncWithAIData внутри себя очистит колоду от карт ИИ, стола и видимого союзника
        _aiData._AIDeck.SyncWithAIData(_aiData);

        snapshot._AIDeck = _aiData._AIDeck;

        // --- 4. ДЕТЕРМИНИЗАЦИЯ РУК ИГРОКОВ (Заполнение скрытых зон) ---
        snapshot.OwnHand = FillHand(_aiData.HandCards, snapshot._AIDeck);
        snapshot.AllyHand = FillHand(_aiData.AllyCards, snapshot._AIDeck);

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

    /// Передает выбранное торговое решение в Player.
    private void ExecuteBettingAction(AIResponsePackage response)
    {
        switch (response.Action)
        {
            case AIActionType.Fold:
                break;
            case AIActionType.CheckCall:
                break;
            case AIActionType.Raise:
                break;
        }
    }

    /// Передает выбранное действие с картами в Player.
    private void ExecuteCardAction(AIResponsePackage response)
    {
        switch (response.Action)
        {
            case AIActionType.DrawCard:
                break;
            case AIActionType.ChangeCombination:
                break;
            case AIActionType.Pass:
                break;
        }
    }

    // --- Система подписок на события ---

    private void SubscribeToEvents()
    {
    }

    private void UnsubscribeFromEvents()
    {
    }

    private void HandleNewRound()
    {
        _aiData?.ClearForNewRound();
    }
}
