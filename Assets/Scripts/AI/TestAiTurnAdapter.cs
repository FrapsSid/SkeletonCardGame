#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGameModel = CardGame;
using CardGameRound = CardGame.Round;

[RequireComponent(typeof(GameManager))]
public sealed class TestAiTurnAdapter : MonoBehaviour
{
    [SerializeField]
    private float actionDelaySeconds = 0.5f;

    private GameManager? gameManager;
    private CardGameModel? subscribedGame;
    private Coroutine? pendingTurn;
    private readonly List<AIController> subscribedAI = new List<AIController>();

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    private void OnEnable()
    {
        gameManager = gameManager != null ? gameManager : GetComponent<GameManager>();
        if (gameManager == null)
            return;

        gameManager.OnGameCreated += HandleGameCreated;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnGameCreated -= HandleGameCreated;

        StopPendingTurn();
        Detach();
    }

    private void HandleGameCreated(CardGameModel game)
    {
        Attach(game);
    }

    private void Attach(CardGameModel? game)
    {
        if (subscribedGame == game)
            return;

        Detach();
        subscribedGame = game;

        if (subscribedGame == null || gameManager == null)
            return;

        subscribedGame.OnCardTaken += OnPlayerCardDealt;
        subscribedGame.OnTableCardsDealt += OnTableCardsDealt;
        subscribedGame.OnRoundEnded += OnRoundEnded;

        foreach (Skeleton player in gameManager.Players)
        {
            if (gameManager.LocalPlayer == player)
                continue;

            if (subscribedGame.TurnStartedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnStarted))
            {
                turnStarted.Fired += HandleTurnStarted;
                subscribedAI.Add(new AIController(new HeuristicDecisionStrategy(), this, player));
            }
        }
    }

    private void Detach()
    {
        if (subscribedGame != null)
        {
            foreach (AIController AI in subscribedAI)
            {
                if (subscribedGame.TurnStartedByPlayer.TryGetValue(AI.player, out CardGameModel.PlayerTurnEvent turnStarted))
                    turnStarted.Fired -= HandleTurnStarted;
            }

            subscribedGame.OnCardTaken -= OnPlayerCardDealt;
            subscribedGame.OnTableCardsDealt -= OnTableCardsDealt;
            subscribedGame.OnRoundEnded -= OnRoundEnded;
        }


        subscribedAI.Clear();
        subscribedGame = null;
    }

    private void HandleTurnStarted(Skeleton player)
    {
        if (gameManager == null || player == gameManager.LocalPlayer)
            return;

        StopPendingTurn();
        pendingTurn = StartCoroutine(ExecuteTurnAfterDelay(player));
    }

    private IEnumerator ExecuteTurnAfterDelay(Skeleton player)
    {
        if (actionDelaySeconds > 0f)
            yield return new WaitForSeconds(actionDelaySeconds);
        else
            yield return null;

        pendingTurn = null;
        foreach (AIController AI in subscribedAI)
        {
            if (AI.player == player)
            {
                AI.ExecuteTurn(subscribedGame?.round);
            }
        }
    }


    /// Передает выбранное торговое решение в Player.
    public void ExecuteCardAction(AIResponsePackage response, Skeleton player)
    {
        CardGameRound? round = subscribedGame?.round;
        if (round == null || round.CurrentPlayer != player || gameManager == null || player == gameManager.LocalPlayer)
            return;

        switch (response.Action)
        {
            case AIActionType.Fold:
                TryFold(round, player);
                return;
            case AIActionType.Pass:
                TryPass(round, player);
                return;
            case AIActionType.DrawCard:
                TryDrawCard(round, player);
                if (gameManager.IsCardDealInProgress)
                {
                    pendingTurn = StartCoroutine(CompleteTurnAfterCardDeal(player));
                    return;
                }
                CompleteTurn(player);
                return;
            case AIActionType.ChangeCombination:
                if(response.ChosenTarget.HasValue)
                {
                    TryMatchCurrentPrice(round, player, response);
                    CompleteTurn(player);
                } else
                {
                    TryFold(round, player);
                }
                return;
        }
    }

    public void ExecuteBettingAction(AIResponsePackage response, Skeleton player)
    {
        CardGameRound? round = subscribedGame?.round;
        if (round == null || round.CurrentPlayer != player || gameManager == null || player == gameManager.LocalPlayer)
            return;

        switch (response.Action)
        {
            case AIActionType.Fold:
                TryFold(round, player);
                break;
            case AIActionType.CheckCall:
                if (response.ChosenTarget.HasValue)
                {
                    TryMatchCurrentPrice(round, player, response);
                }
                else
                {
                    TryFold(round, player);
                }
                break;
        }
    }

    private IEnumerator CompleteTurnAfterCardDeal(Skeleton player)
    {
        while (gameManager != null && gameManager.IsCardDealInProgress)
        {
            yield return null;
        }

        pendingTurn = null;
        CardGameRound? round = subscribedGame?.round;
        if (round == null || round.CurrentPlayer != player || gameManager == null || player == gameManager.LocalPlayer)
            yield break;

        try
        {
            CompleteTurn(player);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Test AI] {PlayerLabel(player)} could not complete a turn after taking a card: {exception.Message}", this);
            TryFold(round, player);
        }
    }

    private void CompleteTurn(Skeleton player)
    {
        CardGameRound? round = subscribedGame?.round;
        if (round == null || round.CurrentPlayer != player)
            return;

        if (round.CurrentPlayer == player && round.HasMatchedBet(player))
            round.EndTurn(player);
    }

    private void TryMatchCurrentPrice(CardGameRound round, Skeleton player, AIResponsePackage responsePackage)
    {
        DeclaredCombinationTier combination = responsePackage.ChosenTarget.HasValue ? responsePackage.ChosenTarget.Value : DeclaredCombinationTier.Easy;

        List<StakeAsset> assets = new List<StakeAsset>(responsePackage.PutOnStakeParts);

        if (round.CanCall(player, assets, combination))
        {
            round.Call(player, assets, combination);
            return;
        }

        if (round.CanAllIn(player, combination))
        {
            round.AllIn(player, combination);
            return;
        }

        TryFold(round, player);
    }

    private void TryFold(CardGameRound round, Skeleton player)
    {
        try
        {
            if (round.ActivePlayers.Count > 1)
                round.Fold(player);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Test AI] {PlayerLabel(player)} could not fold: {exception.Message}", this);
        }
    }

    private void TryPass(CardGameRound round, Skeleton player)
    {
        try
        {
            CompleteTurn(player);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Test AI] {PlayerLabel(player)} could not skip: {exception.Message}", this);
        }
    }

    private void TryDrawCard(CardGameRound round, Skeleton player)
    {
        if(gameManager == null)
        {
            return;
        }

        try
        {
            if (round.CanTakeCard(player))
            {
                round.TakeCard(player);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Test AI] {PlayerLabel(player)} could not complete a turn: {exception.Message}", this);
            TryFold(round, player);
        }
    }


    private void StopPendingTurn()
    {
        if (pendingTurn == null)
            return;

        StopCoroutine(pendingTurn);
        pendingTurn = null;
    }

    private string PlayerLabel(Skeleton player)
    {
        int index = gameManager != null ? IndexOf(gameManager.Players, player) : -1;
        return index >= 0 ? $"Player {index + 1}" : "AI player";
    }

    private static int IndexOf<T>(IReadOnlyList<T> values, T item)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], item))
                return i;
        }

        return -1;
    }

    private void OnPlayerCardDealt(Skeleton player, CardData card)
    {
        foreach(AIController AI in subscribedAI)
        {
            if(AI.player == player)
            {
                AI.OnHandCardDealt(card);
            }
        }
    }

    private void OnTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        foreach (AIController AI in subscribedAI)
        {
            AI.OnTableCardsDealt(cards);
        }
    }

    private void OnRoundEnded(RoundResult roundResult)
    {
        foreach (AIController AI in subscribedAI)
        {
            AI.OnRoundEnded();
        }
    }
}
