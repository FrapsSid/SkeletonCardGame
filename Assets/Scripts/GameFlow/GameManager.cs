#nullable enable
using System.Collections.Generic;
using UnityEngine;
using static CardGame;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool startOnStart = true;

    [SerializeField]
    private BettingDiscussionGate? bettingDiscussionGate;

    private readonly List<Team> teams = new List<Team>();
    private readonly List<Skeleton> players = new List<Skeleton>();
    private bool roundResolved;

    public CardGame? cardGame { get; private set; }
    public BettingDiscussionGate? BettingDiscussionGate => bettingDiscussionGate;
    public MatchScoreTracker ScoreTracker { get; } = new MatchScoreTracker();
    public IReadOnlyList<Team> Teams => teams;
    public IReadOnlyList<Skeleton> Players => players;

    private void Start()
    {
        EnsureBettingDiscussionGate();

        if (startOnStart)
            StartGame();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        UnsubscribeFromDiscussionManager();
        bettingDiscussionGate?.StopDiscussion();
    }

    public void StartGame()
    {
        Unsubscribe();
        UnsubscribeFromDiscussionManager();
        EnsureBettingDiscussionGate();
        bettingDiscussionGate?.StopDiscussion();
        CreateTestPlayers();

        cardGame = new CardGame(teams, players);
        Subscribe(cardGame);
        SubscribeToDiscussionManager();

        cardGame.DealPlayersCards();
        cardGame.ShowCombinations();
        cardGame.StartRound();
    }

    private void CreateTestPlayers()
    {
        teams.Clear();
        players.Clear();
        roundResolved = false;

        Team firstTeam = CreateTestTeam();
        Team secondTeam = CreateTestTeam();

        teams.Add(firstTeam);
        teams.Add(secondTeam);

        players.Add(CreatePlayer(firstTeam));
        players.Add(CreatePlayer(secondTeam));
    }

    private static Team CreateTestTeam()
    {
        var team = new Team();
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 1));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 2));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.OtherTeamAsset, 3));
        return team;
    }

    private static Skeleton CreatePlayer(Team team)
    {
        var player = new Skeleton(team);
        team.AddSkeleton(player);
        return player;
    }

    private void Subscribe(CardGame game)
    {
        game.OnPhaseChanged += HandlePhaseChanged;
        game.OnTurnStarted += HandleTurnStarted;
        game.OnRoundEnded += HandleRoundEnded;
    }

    private void Unsubscribe()
    {
        if (cardGame == null)
            return;

        cardGame.OnPhaseChanged -= HandlePhaseChanged;
        cardGame.OnTurnStarted -= HandleTurnStarted;
        cardGame.OnRoundEnded -= HandleRoundEnded;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (cardGame?.round == null)
            return;

        if (phase == GamePhase.BettingRoundStart)
        {
            bettingDiscussionGate?.StartPostDealDiscussion(cardGame.round);
        }
        else if (phase == GamePhase.AddingCards)
        {
            cardGame.DealTableCards();
        }
        else if (phase == GamePhase.End && !roundResolved)
        {
            roundResolved = true;
            cardGame.round.DetermineWinners();
            cardGame.round.ResolvePot();
        }
    }

    private void HandleTurnStarted(Skeleton player)
    {
        Debug.Log($"Turn started: {players.IndexOf(player)}", this);
    }

    private void HandleRoundEnded(RoundResult result)
    {
        ScoreTracker.AddRoundResult(result);
    }

    private void HandleDiscussionCompleted(Round round)
    {
        if (cardGame?.round != round || cardGame.phase != GamePhase.BettingRoundStart)
            return;

        cardGame.StartBettingRound();
    }

    private void EnsureBettingDiscussionGate()
    {
        if (bettingDiscussionGate != null)
            return;

        bettingDiscussionGate = GetComponent<BettingDiscussionGate>();
        if (bettingDiscussionGate == null)
            bettingDiscussionGate = gameObject.AddComponent<BettingDiscussionGate>();
    }

    private void SubscribeToDiscussionManager()
    {
        if (bettingDiscussionGate != null)
            bettingDiscussionGate.OnDiscussionCompleted += HandleDiscussionCompleted;
    }

    private void UnsubscribeFromDiscussionManager()
    {
        if (bettingDiscussionGate != null)
            bettingDiscussionGate.OnDiscussionCompleted -= HandleDiscussionCompleted;
    }
}
