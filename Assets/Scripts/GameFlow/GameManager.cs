#nullable enable

using System.Collections.Generic;
using UnityEngine;

using CardGameModel = Assets.Scripts.CardGame.CardGame;
using GamePhase = Assets.Scripts.CardGame.CardGame.GamePhase;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool startOnStart = true;

    private readonly List<Team> teams = new List<Team>();
    private readonly List<Skeleton> players = new List<Skeleton>();
    private bool roundResolved;

    public CardGameModel? CardGame { get; private set; }
    public MatchScoreTracker ScoreTracker { get; } = new MatchScoreTracker();
    public IReadOnlyList<Team> Teams => teams;
    public IReadOnlyList<Skeleton> Players => players;

    private void Start()
    {
        if (startOnStart)
            StartGame();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void StartGame()
    {
        Unsubscribe();
        CreateTestPlayers();

        CardGame = new CardGameModel(teams, players);
        Subscribe(CardGame);

        CardGame.DealPlayersCards();
        CardGame.ShowCombinations();
        CardGame.StartRound();
        CardGame.StartBettingRound();
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

    private void Subscribe(CardGameModel game)
    {
        game.OnPhaseChanged += HandlePhaseChanged;
        game.OnTurnStarted += HandleTurnStarted;
        game.OnRoundEnded += HandleRoundEnded;
    }

    private void Unsubscribe()
    {
        if (CardGame == null)
            return;

        CardGame.OnPhaseChanged -= HandlePhaseChanged;
        CardGame.OnTurnStarted -= HandleTurnStarted;
        CardGame.OnRoundEnded -= HandleRoundEnded;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (CardGame?.round == null)
            return;

        if (phase == GamePhase.AddingCards)
        {
            CardGame.DealTableCards();
            CardGame.StartBettingRound();
        }
        else if (phase == GamePhase.End && !roundResolved)
        {
            roundResolved = true;
            CardGame.round.DetermineWinners();
            CardGame.round.ResolvePot();
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
}
