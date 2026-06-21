#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardGame;

[RequireComponent(typeof(BettingDiscussionGate))]
public sealed class GameManager : MonoBehaviour
{
    private readonly List<Team> teams = new List<Team>();
    private readonly List<Skeleton> players = new List<Skeleton>();
    private BettingDiscussionGate? bettingDiscussionGate;
    private Coroutine? restartRoundCoroutine;
    private bool roundResolved;

    public CardGame? CardGame { get; private set; }
    public Skeleton? CurrentPlayer { get; private set; }
    public Skeleton? HumanPlayer { get; private set; }
    public IReadOnlyList<Team> Teams => teams;
    public IReadOnlyList<Skeleton> Players => players;

    public event Action<CardGame>? OnGameCreated;

    private void Awake()
    {
        bettingDiscussionGate = GetComponent<BettingDiscussionGate>();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        UnsubscribeFromDiscussionGate();
        StopRestartRound();
        bettingDiscussionGate?.StopDiscussion();
    }

    public void StartGame(IEnumerable<Team> gameTeams, IEnumerable<Skeleton> gamePlayers)
    {
        if (gameTeams == null)
            throw new ArgumentNullException(nameof(gameTeams));
        if (gamePlayers == null)
            throw new ArgumentNullException(nameof(gamePlayers));

        teams.Clear();
        players.Clear();
        teams.AddRange(gameTeams);
        players.AddRange(gamePlayers);
        HumanPlayer = players.Count > 0 ? players[0] : null;

        StartGame();
    }

    public void StartGame()
    {
        if (teams.Count == 0 || players.Count == 0)
            throw new InvalidOperationException("GameManager needs teams and players before starting a game.");

        Unsubscribe();
        UnsubscribeFromDiscussionGate();
        StopRestartRound();

        CurrentPlayer = null;
        bettingDiscussionGate = GetRequiredBettingDiscussionGate();
        SubscribeToDiscussionGate();
        roundResolved = false;

        CardGame = new CardGame(teams, players);
        Subscribe(CardGame);
        OnGameCreated?.Invoke(CardGame);
        StartRoundFlow(CardGame);
    }

    private void StartRoundFlow(CardGame game)
    {
        game.DealPlayersCards();
        game.ShowCombinations();
        game.StartRound();
    }

    private void Subscribe(CardGame game)
    {
        game.OnPhaseChanged += HandlePhaseChanged;
        game.OnTurnStarted += HandleTurnStarted;
        game.OnTurnEnded += HandleTurnEnded;
        game.OnRoundEnded += HandleRoundEnded;
    }

    private void Unsubscribe()
    {
        if (CardGame == null)
            return;

        CardGame.OnPhaseChanged -= HandlePhaseChanged;
        CardGame.OnTurnStarted -= HandleTurnStarted;
        CardGame.OnTurnEnded -= HandleTurnEnded;
        CardGame.OnRoundEnded -= HandleRoundEnded;
    }

    public bool IsHumanPlayer(Skeleton? player)
    {
        return player != null && HumanPlayer != null && ReferenceEquals(HumanPlayer, player);
    }

    public bool IsAiPlayer(Skeleton? player)
    {
        return player != null && HumanPlayer != null && !ReferenceEquals(HumanPlayer, player);
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        CardGame? game = CardGame;
        if (game?.round == null)
            return;

        if (phase == GamePhase.BettingRoundStart)
        {
            StartBettingDiscussion(game.round);
        }
        else if (phase == GamePhase.AddingCards)
        {
            game.DealTableCards();
        }
        else if (phase == GamePhase.End && !roundResolved)
        {
            roundResolved = true;
            game.round.DetermineWinners();
            game.round.ResolvePot();
        }
    }

    private void HandleTurnStarted(Skeleton player)
    {
        CurrentPlayer = player;
        Debug.Log($"Turn started: {players.IndexOf(player)}", this);
    }

    private void HandleTurnEnded(Skeleton player)
    {
        if (ReferenceEquals(CurrentPlayer, player))
            CurrentPlayer = null;
    }

    private void HandleRoundEnded(RoundResult result)
    {
        StopRestartRound();
        restartRoundCoroutine = StartCoroutine(RestartRoundAfterRoundEnded());
    }

    private IEnumerator RestartRoundAfterRoundEnded()
    {
        yield return null;
        restartRoundCoroutine = null;

        CardGame? game = CardGame;
        if (game == null || game.phase != GamePhase.End)
            yield break;

        game.ResetRound();
        CurrentPlayer = null;
        roundResolved = false;
        StartRoundFlow(game);
    }

    private void StartBettingDiscussion(Round round)
    {
        BettingDiscussionGate gate = GetRequiredBettingDiscussionGate();
        gate.StopDiscussion();
        gate.StartPostDealDiscussion(round);
    }

    private void HandleDiscussionCompleted(Round round)
    {
        CardGame? game = CardGame;
        if (game?.round != round || game.phase != GamePhase.BettingRoundStart)
            return;

        game.StartBettingRound();
    }

    private void SubscribeToDiscussionGate()
    {
        if (bettingDiscussionGate != null)
            bettingDiscussionGate.OnDiscussionCompleted += HandleDiscussionCompleted;
    }

    private void UnsubscribeFromDiscussionGate()
    {
        if (bettingDiscussionGate != null)
            bettingDiscussionGate.OnDiscussionCompleted -= HandleDiscussionCompleted;
    }

    private BettingDiscussionGate GetRequiredBettingDiscussionGate()
    {
        if (bettingDiscussionGate != null)
            return bettingDiscussionGate;

        bettingDiscussionGate = GetComponent<BettingDiscussionGate>();
        if (bettingDiscussionGate == null)
            throw new InvalidOperationException("GameManager requires a BettingDiscussionGate component.");

        return bettingDiscussionGate;
    }

    private void StopRestartRound()
    {
        if (restartRoundCoroutine == null)
            return;

        StopCoroutine(restartRoundCoroutine);
        restartRoundCoroutine = null;
    }
}
