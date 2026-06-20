using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    //[Header("Dependencies")]
    //[SerializeField] private PlayerInterface playerInterface;
    //[SerializeField] private PlayerData playerData;

    // --- Поля архитектуры ИИ ---
    private AIData _aiData;
    private BaseAIDecisionStrategy _decisionStrategy;

    private bool _isThinking;

    /// <summary>
    /// Инициализация контроллера ИИ. Сюда передается конкретная стратегия (Эвристика или MCTS).
    /// </summary>
    public void Initialize(BaseAIDecisionStrategy strategy)
    {
        _decisionStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        // Инициализируем контейнер AIData, передавая физический скелет ИИ-игрока
        _aiData = new AIData(null);

        // Подписываемся на необходимые методы и события игры
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся во избежание утечек памяти
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Главный метод выполнения хода. Вызывается Игровым Менеджером, когда наступает очередь этого бота.
    /// </summary>
    /// <param name="isBettingPhase">true — если сейчас фаза торгов (активная), false — если фаза ходов картами (пассивная)</param>
    public void ExecuteTurn(bool isBettingPhase)
    {
    }

    /// <summary>
    /// Внутренний рабочий цикл размышления бота.
    /// </summary>
    private IEnumerator ExecuteTurnRoutine(bool isBettingPhase)
    {
        _isThinking = true;

        _isThinking = false;

        yield break;
    }

    /// <summary>
    /// Упаковывает информацию из AIData и систем игры в GameStateSnapshot, 
    /// центрируя все массивы рук и целей вокруг текущего бота (OwnHand).
    /// </summary>
    private GameStateSnapshot CreateSnapshotFromAIData()
    {
        return new GameStateSnapshot();
    }

    /// <summary>
    /// Передает выбранное торговое решение в PlayerInterface.
    /// </summary>
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

    /// <summary>
    /// Передает выбранное действие с картами в PlayerInterface.
    /// </summary>
    private void ExecuteCardAction(AIResponsePackage response)
    {
        switch (response.Action)
        {
            case AIActionType.CheckCall:
                break;
            case AIActionType.DrawCard:
                break;
            case AIActionType.ChangeCombination:
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
