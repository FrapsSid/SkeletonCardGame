#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardGame;

[RequireComponent(typeof(BettingDiscussionGate))]
public sealed class GameManager : MonoBehaviour
{
    private readonly List<Team> _teams = new();
    private readonly List<Skeleton> _players = new();
    private BettingDiscussionGate _bettingDiscussionGate = null!;
    private Coroutine? _restartRoundCoroutine;
    private bool _roundResolved;

    public CardGame? CardGame { get; private set; }
    public Skeleton? CurrentPlayer { get; private set; }
    public IReadOnlyList<Team> Teams => _teams;
    public IReadOnlyList<Skeleton> Players => _players;
    public event Action<CardGame>? OnGameCreated;

    private void Awake()
    {
        _bettingDiscussionGate = GetComponent<BettingDiscussionGate>() ?? throw new NullReferenceException(nameof(BettingDiscussionGate));
    }

    private void OnDestroy()
    {
        Unsubscribe();
        UnsubscribeFromDiscussionGate();
        StopRestartRound();
        _bettingDiscussionGate.StopDiscussion();
    }

    public void StartGame(IEnumerable<Team> gameTeams, IEnumerable<Skeleton> gamePlayers, Skeleton currentPlayer)
    {
        if (gameTeams == null)
            throw new ArgumentNullException(nameof(gameTeams));
        if (gamePlayers == null)
            throw new ArgumentNullException(nameof(gamePlayers));

        _teams.Clear();
        _players.Clear();
        _teams.AddRange(gameTeams);
        _players.AddRange(gamePlayers);
        CurrentPlayer = currentPlayer;

        StartGame();
    }

    public void StartGame()
    {
        if (_teams.Count == 0 || _players.Count == 0)
            throw new InvalidOperationException("GameManager needs teams and players before starting a game.");

        Unsubscribe();
        UnsubscribeFromDiscussionGate();
        StopRestartRound();

        CurrentPlayer = null;
        SubscribeToDiscussionGate();
        _roundResolved = false;

        CardGame = new CardGame(_teams, _players);
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
        game.OnRoundEnded += HandleRoundEnded;
    }

    private void Unsubscribe()
    {
        if (CardGame == null)
            return;

        CardGame.OnPhaseChanged -= HandlePhaseChanged;
        CardGame.OnRoundEnded -= HandleRoundEnded;
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
        else if (phase == GamePhase.End && !_roundResolved)
        {
            _roundResolved = true;
            game.round.DetermineWinners();
            game.round.ResolvePot();
        }
    }
    private void HandleRoundEnded(RoundResult result)
    {
        StopRestartRound();
        _restartRoundCoroutine = StartCoroutine(RestartRoundAfterRoundEnded());
    }

    private IEnumerator RestartRoundAfterRoundEnded()
    {
        yield return null;
        _restartRoundCoroutine = null;

        CardGame? game = CardGame;
        if (game == null || game.phase != GamePhase.End)
            yield break;

        game.ResetRound();
        CurrentPlayer = null;
        _roundResolved = false;
        StartRoundFlow(game);
    }

    private void StartBettingDiscussion(Round round)
    {
        _bettingDiscussionGate.StopDiscussion();
        _bettingDiscussionGate.StartPostDealDiscussion(round);
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
        _bettingDiscussionGate.OnDiscussionCompleted += HandleDiscussionCompleted;
    }

    private void UnsubscribeFromDiscussionGate()
    {
        _bettingDiscussionGate.OnDiscussionCompleted -= HandleDiscussionCompleted;
    }

    private void StopRestartRound()
    {
        if (_restartRoundCoroutine == null)
            return;

        StopCoroutine(_restartRoundCoroutine);
        _restartRoundCoroutine = null;
    }
}
