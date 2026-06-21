#nullable enable
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameManager))]
public sealed class GameManagerTestBootstrapper : MonoBehaviour
{
    [SerializeField]
    private bool startOnStart = true;

    private GameManager? gameManager;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    private void Start()
    {
        if (startOnStart)
            StartTestGame();
    }

    public void StartTestGame()
    {
        GameManager manager = gameManager != null ? gameManager : GetComponent<GameManager>();
        gameManager = manager;

        var teams = new List<Team>();
        var players = new List<Skeleton>();

        Team firstTeam = CreateTestTeam();
        Team secondTeam = CreateTestTeam();

        teams.Add(firstTeam);
        teams.Add(secondTeam);

        players.Add(CreatePlayer(firstTeam));
        players.Add(CreatePlayer(secondTeam));

        manager.StartGame(teams, players);
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
}
