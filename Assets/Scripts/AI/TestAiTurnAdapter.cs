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
    private float actionDelaySeconds = 0.25f;

    private GameManager? gameManager;
    private CardGameModel? subscribedGame;
    private Coroutine? pendingTurn;
    private readonly List<Skeleton> subscribedPlayers = new List<Skeleton>();

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
        Attach(gameManager.CardGame);
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

        foreach (Skeleton player in gameManager.Players)
        {
            if (gameManager.LocalPlayer == player)
                continue;

            if (subscribedGame.TurnStartedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnStarted))
            {
                turnStarted.Fired += HandleTurnStarted;
                subscribedPlayers.Add(player);
            }
        }
    }

    private void Detach()
    {
        if (subscribedGame != null)
        {
            foreach (Skeleton player in subscribedPlayers)
            {
                if (subscribedGame.TurnStartedByPlayer.TryGetValue(player, out CardGameModel.PlayerTurnEvent turnStarted))
                    turnStarted.Fired -= HandleTurnStarted;
            }
        }

        subscribedPlayers.Clear();
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
        ExecuteAiTurn(player);
    }

    private void ExecuteAiTurn(Skeleton player)
    {
        CardGameRound? round = subscribedGame?.round;
        if (round == null || round.CurrentPlayer != player || gameManager == null || player == gameManager.LocalPlayer)
            return;

        try
        {
            if (round.CanTakeCard(player))
                round.TakeCard(player);

            if (!round.HasMatchedBet(player))
                MatchCurrentPrice(round, player);

            if (round.CurrentPlayer == player && round.HasMatchedBet(player))
                round.EndTurn(player);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Test AI] {PlayerLabel(player)} could not complete a turn: {exception.Message}", this);
            TryFold(round, player);
        }
    }

    private void MatchCurrentPrice(CardGameRound round, Skeleton player)
    {
        List<StakeAsset> assets = player.team.Assets
            .Where(asset => asset != null && player.team.OwnsAsset(asset))
            .ToList();

        if (round.CanCall(player, assets, DeclaredCombinationTier.Easy))
        {
            round.Call(player, assets, DeclaredCombinationTier.Easy);
            return;
        }

        if (round.CanAllIn(player, DeclaredCombinationTier.Easy))
        {
            round.AllIn(player, DeclaredCombinationTier.Easy);
            return;
        }

        TryFold(round, player);
    }

    private void TryFold(CardGameRound round, Skeleton player)
    {
        try
        {
            if (round.CurrentPlayer == player && round.ActivePlayers.Count > 1)
                round.Fold(player);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Test AI] {PlayerLabel(player)} could not fold: {exception.Message}", this);
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
}
